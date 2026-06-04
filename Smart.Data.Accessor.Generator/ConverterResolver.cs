namespace Smart.Data.Accessor.Generator;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.GeneratorShared;

using SourceGenerateHelper;

// Resolves and validates [TypeHandler<TConverter>] / [TypeHandler(typeof(TConverter))] across the
// scope chain: member (property / parameter / [return:]) → method → accessor class → [ExecuteConfig]
// profile. A converter implements IValueConverter<TDb, TClr>; the reader reads TDb and calls
// TConverter.FromDb(...) while the writer calls TConverter.ToDb(...).
// The member scope is an explicit binding (a TClr mismatch is the error SDA0308); the outer scopes
// are type-keyed (a handler applies only when its TClr matches the value type, and a non-matching
// handler is simply skipped). Reports SDA0308–SDA0311.
// The scope-chain primitives (attribute reading / type-keyed find / TDb·TClr extraction / Nullable
// unwrap) are shared with the Builder generator via ConverterScopeHelper; only the ordering and the
// validation are core-specific.
internal static class ConverterResolver
{
    // The converter binding resolved for a single mapped value (column / parameter / scalar).
    // DbType = TDb, ClrType = TClr of IValueConverter<TDb, TClr>.
    internal sealed record Result(string ConverterTypeFullName, ITypeSymbol DbType, ITypeSymbol ClrType);

    // The outer scope owners consulted (in order) when the member itself carries no [TypeHandler]:
    // method → accessor class → profile. Profile is the type referenced by [ExecuteConfig(typeof(P))]
    // (null when absent).
    internal readonly record struct Scope(IMethodSymbol Method, INamedTypeSymbol AccessorClass, INamedTypeSymbol? Profile);

    // Resolves the converter to apply to a member of type valueType. Returns null when no handler
    // applies or when validation fails (a diagnostic is reported in the latter case so the
    // mapping/binding falls back to the plain path).
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
