namespace Smart.Data.Accessor.Generator;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Generator.Models;

/// <summary>
/// Resolves and validates <c>[TypeHandler&lt;TConverter&gt;]</c> / <c>[TypeHandler(typeof(TConverter))]</c>
/// across the scope chain defined in spec §7.7: member (property / parameter / <c>[return:]</c>) →
/// method → accessor class → <c>[ExecuteConfig]</c> profile. A converter implements
/// <c>IValueConverter&lt;TDb, TClr&gt;</c>; the reader reads <c>TDb</c> and calls
/// <c>TConverter.FromDb(...)</c> while the writer calls <c>TConverter.ToDb(...)</c>.
/// The member scope is an <em>explicit</em> binding (a TClr mismatch is the error SDA0142); the
/// outer scopes are <em>type-keyed</em> (a handler applies only when its TClr matches the value
/// type, and a non-matching handler is simply skipped). Reports SDA0142–SDA0145.
/// </summary>
internal static class ConverterResolver
{
    private const string TypeHandlerGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute<TConverter>";
    private const string TypeHandlerNonGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute";
    private const string IValueConverterFq = "Smart.Data.Accessor.Converters.IValueConverter<TDb, TClr>";

    /// <summary>The converter binding resolved for a single mapped value (column / parameter / scalar).</summary>
    internal sealed record Result(string ConverterTypeFullName, ITypeSymbol DbType);

    /// <summary>
    /// The outer scope owners consulted (in spec §7.7 order) when the member itself carries no
    /// <c>[TypeHandler]</c>: method → accessor class → profile. <paramref name="Profile"/> is the
    /// type referenced by <c>[ExecuteConfig(typeof(P))]</c> (null when absent).
    /// </summary>
    internal readonly record struct Scope(IMethodSymbol Method, INamedTypeSymbol AccessorClass, INamedTypeSymbol? Profile);

    /// <summary>
    /// Resolves the converter to apply to a member of type <paramref name="valueType"/>.
    /// <paramref name="memberAttributes"/> are the member's own attributes (a property's, a
    /// parameter's, or <c>method.GetReturnTypeAttributes()</c> for <c>[return:]</c>); when they carry
    /// no <c>[TypeHandler]</c> the resolution falls through to the type-keyed outer scopes.
    /// Returns <c>null</c> when no handler applies or when validation fails (a diagnostic is reported
    /// in the latter case so the mapping/binding falls back to the plain path).
    /// </summary>
    public static Result? Resolve(
        List<DiagnosticData> diagnostics,
        IMethodSymbol method,
        string memberName,
        ImmutableArray<AttributeData> memberAttributes,
        ITypeSymbol valueType,
        in Scope scope)
    {
        // 1. Member scope (explicit binding): the first [TypeHandler] wins and TClr must match.
        var memberConverters = CollectHandlerConverters(memberAttributes);
        if (memberConverters.Count > 0)
        {
            // SDA0145: more than one [TypeHandler] on the same member; the first wins.
            if (memberConverters.Count > 1)
            {
                diagnostics.Add(DiagnosticData.Create(
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
            if (FindTypeKeyedConverter(owner.GetAttributes(), valueType) is { } scopedConverter)
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

    // The first [TypeHandler]-family handler at this scope whose IValueConverter TClr matches the
    // value type (Nullable<T> compares against T). Non-matching handlers apply to other types and
    // are skipped without a diagnostic.
    private static INamedTypeSymbol? FindTypeKeyedConverter(ImmutableArray<AttributeData> attributes, ITypeSymbol valueType)
    {
        var underlying = UnwrapNullable(valueType);
        foreach (var converter in CollectHandlerConverters(attributes))
        {
            if (converter is not null &&
                TryGetConverterTypes(converter, out _, out var clrType) &&
                SymbolEqualityComparer.Default.Equals(clrType, underlying))
            {
                return converter;
            }
        }
        return null;
    }

    // Validates a selected converter and, on success, returns its binding. requireClrMatch is true
    // only for the explicit member scope (a mismatch there is SDA0142); the type-keyed scopes have
    // already filtered on TClr so a mismatch cannot occur.
    private static Result? Validate(
        List<DiagnosticData> diagnostics,
        IMethodSymbol method,
        string memberName,
        INamedTypeSymbol converter,
        ITypeSymbol valueType,
        bool requireClrMatch)
    {
        // SDA0143: the referenced converter type does not implement IValueConverter<,>.
        if (!TryGetConverterTypes(converter, out var dbType, out var clrType))
        {
            diagnostics.Add(DiagnosticData.Create(
                Diagnostics.ConverterNotIValueConverter,
                method.Locations.FirstOrDefault(),
                method.Name,
                converter.ToDisplayString()));
            return null;
        }

        // SDA0142: the converter's TClr must match the member type (Nullable<T> compares against T).
        if (!SymbolEqualityComparer.Default.Equals(clrType, UnwrapNullable(valueType)))
        {
            if (requireClrMatch)
            {
                diagnostics.Add(DiagnosticData.Create(
                    Diagnostics.ConverterTClrMismatch,
                    method.Locations.FirstOrDefault(),
                    method.Name,
                    memberName));
            }
            return null;
        }

        // SDA0144: the generated code calls TConverter.FromDb / .ToDb directly, so both must be
        // present as accessible static methods (implicit interface implementation, not explicit).
        if (!HasCallableStatic(converter, "FromDb") || !HasCallableStatic(converter, "ToDb"))
        {
            diagnostics.Add(DiagnosticData.Create(
                Diagnostics.ConverterStaticAbstractMissing,
                method.Locations.FirstOrDefault(),
                method.Name,
                converter.ToDisplayString()));
            return null;
        }

        return new Result(converter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), dbType);
    }

    private static bool TryGetConverterTypes(INamedTypeSymbol converter, out ITypeSymbol dbType, out ITypeSymbol clrType)
    {
        var iface = converter.AllInterfaces.FirstOrDefault(static i =>
            i.IsGenericType && i.ConstructedFrom.ToDisplayString() == IValueConverterFq);
        if (iface is null)
        {
            dbType = null!;
            clrType = null!;
            return false;
        }
        dbType = iface.TypeArguments[0];
        clrType = iface.TypeArguments[1];
        return true;
    }

    // All converter types referenced by [TypeHandler]-family attributes, in source order. An entry
    // is null when the attribute's converter type could not be resolved.
    private static List<INamedTypeSymbol?> CollectHandlerConverters(ImmutableArray<AttributeData> attributes)
    {
        var converters = new List<INamedTypeSymbol?>();
        foreach (var attr in attributes)
        {
            for (var current = attr.AttributeClass; current is not null; current = current.BaseType)
            {
                if (current.IsGenericType && current.ConstructedFrom.ToDisplayString() == TypeHandlerGenericFq)
                {
                    converters.Add(current.TypeArguments[0] as INamedTypeSymbol);
                    break;
                }
                if (current.ToDisplayString() == TypeHandlerNonGenericFq)
                {
                    converters.Add(attr.ConstructorArguments.Length > 0
                        ? attr.ConstructorArguments[0].Value as INamedTypeSymbol
                        : null);
                    break;
                }
            }
        }
        return converters;
    }

    private static bool HasCallableStatic(INamedTypeSymbol converter, string name) =>
        converter.GetMembers(name).OfType<IMethodSymbol>().Any(static m =>
            m.IsStatic &&
            m.MethodKind == MethodKind.Ordinary &&
            m.DeclaredAccessibility == Accessibility.Public);

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return nt.TypeArguments[0];
        }
        return type;
    }
}
