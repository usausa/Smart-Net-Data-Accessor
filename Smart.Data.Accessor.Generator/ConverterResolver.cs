namespace Smart.Data.Accessor.Generator;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.GeneratorShared;

using SourceGenerateHelper;

/// <summary>
/// Resolves and validates <c>[TypeHandler&lt;TConverter&gt;]</c> / <c>[TypeHandler(typeof(TConverter))]</c>
/// across the scope chain defined in spec §7.7: member (property / parameter / <c>[return:]</c>) →
/// method → accessor class → <c>[ExecuteConfig]</c> profile. A converter implements
/// <c>IValueConverter&lt;TDb, TClr&gt;</c>; the reader reads <c>TDb</c> and calls
/// <c>TConverter.FromDb(...)</c> while the writer calls <c>TConverter.ToDb(...)</c>.
/// The member scope is an <em>explicit</em> binding (a TClr mismatch is the error SDA0308); the
/// outer scopes are <em>type-keyed</em> (a handler applies only when its TClr matches the value
/// type, and a non-matching handler is simply skipped). Reports SDA0308–SDA0311.
/// The scope-chain primitives (attribute reading / type-keyed find / TDb·TClr extraction / Nullable
/// unwrap) are shared with the Builder generator via <see cref="ConverterScopeHelper"/> (改善2 ②); only the
/// ordering and the validation (SDA0308-0145) are core-specific.
/// </summary>
internal static class ConverterResolver
{
    /// <summary>The converter binding resolved for a single mapped value (column / parameter / scalar).</summary>
    /// <remarks><see cref="DbType"/> = TDb, <see cref="ClrType"/> = TClr of IValueConverter&lt;TDb, TClr&gt;.</remarks>
    internal sealed record Result(string ConverterTypeFullName, ITypeSymbol DbType, ITypeSymbol ClrType);

    /// <summary>
    /// The outer scope owners consulted (in spec §7.7 order) when the member itself carries no
    /// <c>[TypeHandler]</c>: method → accessor class → profile. <paramref name="Profile"/> is the
    /// type referenced by <c>[ExecuteConfig(typeof(P))]</c> (null when absent).
    /// </summary>
    internal readonly record struct Scope(IMethodSymbol Method, INamedTypeSymbol AccessorClass, INamedTypeSymbol? Profile);

    /// <summary>
    /// Resolves the converter to apply to a member of type <paramref name="valueType"/>.
    /// Returns <c>null</c> when no handler applies or when validation fails (a diagnostic is reported
    /// in the latter case so the mapping/binding falls back to the plain path).
    /// </summary>
    public static Result? Resolve(
        List<DiagnosticInfo> diagnostics,
        IMethodSymbol method,
        string memberName,
        ImmutableArray<AttributeData> memberAttributes,
        ITypeSymbol valueType,
        in Scope scope)
    {
        // 1. Member scope (explicit binding): the first [TypeHandler] wins and TClr must match.
        var memberConverters = ConverterScopeHelper.CollectHandlerConverters(memberAttributes);
        if (memberConverters.Count > 0)
        {
            // SDA0311: more than one [TypeHandler] on the same member; the first wins.
            if (memberConverters.Count > 1)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.TypeHandlerDuplicated,
                    method.Locations.FirstOrDefault(),
                    method.Name,
                    memberName));
            }

            return memberConverters[0] is { } explicitConverter
                ? Validate(diagnostics, method, memberName, explicitConverter, valueType, requireClrMatch: true)
                : null;
        }

        // 2-4. Type-keyed outer scopes: method → accessor class → profile. The first scope that
        // declares a handler whose TClr matches the value type wins.
        foreach (var owner in EnumerateScopeOwners(scope))
        {
            if (ConverterScopeHelper.FindTypeKeyedConverter(owner.GetAttributes(), valueType) is { } scopedConverter)
            {
                return Validate(diagnostics, method, memberName, scopedConverter, valueType, requireClrMatch: false);
            }
        }

        return null;
    }

    private static IEnumerable<ISymbol> EnumerateScopeOwners(Scope scope)
    {
        yield return scope.Method;
        yield return scope.AccessorClass;
        if (scope.Profile is not null)
        {
            yield return scope.Profile;
        }
    }

    // Validates a selected converter and, on success, returns its binding. requireClrMatch is true
    // only for the explicit member scope (a mismatch there is SDA0308); the type-keyed scopes have
    // already filtered on TClr so a mismatch cannot occur.
    private static Result? Validate(
        List<DiagnosticInfo> diagnostics,
        IMethodSymbol method,
        string memberName,
        INamedTypeSymbol converter,
        ITypeSymbol valueType,
        bool requireClrMatch)
    {
        // SDA0309: the referenced converter type does not implement IValueConverter<,>.
        if (!ConverterScopeHelper.TryGetConverterTypes(converter, out var dbType, out var clrType))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.ConverterNotIValueConverter,
                method.Locations.FirstOrDefault(),
                method.Name,
                converter.ToDisplayString()));
            return null;
        }

        // SDA0308: the converter's TClr must match the member type (Nullable<T> compares against T).
        if (!ConverterScopeHelper.ClrMatchesValueType(clrType, valueType))
        {
            if (requireClrMatch)
            {
                diagnostics.Add(new DiagnosticInfo(
                    Diagnostics.ConverterTClrMismatch,
                    method.Locations.FirstOrDefault(),
                    method.Name,
                    memberName));
            }
            return null;
        }

        // SDA0310: the generated code calls TConverter.FromDb / .ToDb directly, so both must be
        // present as accessible static methods (implicit interface implementation, not explicit).
        if (!ConverterScopeHelper.HasCallableConversionMethods(converter))
        {
            diagnostics.Add(new DiagnosticInfo(
                Diagnostics.ConverterStaticAbstractMissing,
                method.Locations.FirstOrDefault(),
                method.Name,
                converter.ToDisplayString()));
            return null;
        }

        return new Result(converter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), dbType, clrType);
    }
}
