namespace Smart.Data.Accessor.Generator;

using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

/// <summary>
/// Resolves and validates <c>[TypeHandler&lt;TConverter&gt;]</c> / <c>[TypeHandler(typeof(TConverter))]</c>
/// for the reader (mapping) side (spec §7.4 / §7.10). A converter implements
/// <c>IValueConverter&lt;TDb, TClr&gt;</c>; the generated mapping reads <c>TDb</c> from the reader and
/// calls <c>TConverter.FromDb(...)</c> to produce the <c>TClr</c> property value.
/// Reports SDA0141–SDA0145 when the converter is malformed.
/// </summary>
internal static class ConverterResolver
{
    private const string TypeHandlerGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute<TConverter>";
    private const string TypeHandlerNonGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute";
    private const string IValueConverterFq = "Smart.Data.Accessor.Converters.IValueConverter<TDb, TClr>";
    private const string ConverterSupportedTypesFq = "Smart.Data.Accessor.Attributes.ConverterSupportedTypesAttribute";

    /// <summary>The converter binding resolved for a single mapped column.</summary>
    internal sealed record Result(string ConverterTypeFullName, ITypeSymbol DbType);

    /// <summary>
    /// Resolves the converter to apply to <paramref name="memberSymbol"/> (a property or record
    /// primary-constructor parameter). Returns <c>null</c> when there is no <c>[TypeHandler]</c> or
    /// when validation fails (a diagnostic is reported in the latter case so the mapping falls back
    /// to the plain column read, avoiding a confusing secondary compile error).
    /// </summary>
    public static Result? Resolve(
        SourceProductionContext context,
        IMethodSymbol method,
        ISymbol memberSymbol,
        ITypeSymbol propertyType)
    {
        var handlers = CollectHandlerConverters(memberSymbol);
        if (handlers.Count == 0)
        {
            return null;
        }

        // SDA0145: more than one [TypeHandler] on the same property; the first wins.
        if (handlers.Count > 1)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.TypeHandlerDuplicated,
                method.Locations.FirstOrDefault(),
                method.Name,
                memberSymbol.Name));
        }

        if (handlers[0] is not { } converter)
        {
            return null;
        }

        // SDA0143: the referenced converter type does not implement IValueConverter<,>.
        var iface = converter.AllInterfaces.FirstOrDefault(static i =>
            i.IsGenericType && i.ConstructedFrom.ToDisplayString() == IValueConverterFq);
        if (iface is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ConverterNotIValueConverter,
                method.Locations.FirstOrDefault(),
                method.Name,
                converter.ToDisplayString()));
            return null;
        }

        var dbType = iface.TypeArguments[0];
        var clrType = iface.TypeArguments[1];

        // SDA0142: the converter's TClr must match the property type (Nullable<T> compares against T).
        var propUnderlying = UnwrapNullable(propertyType);
        if (!SymbolEqualityComparer.Default.Equals(clrType, propUnderlying))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ConverterTClrMismatch,
                method.Locations.FirstOrDefault(),
                method.Name,
                memberSymbol.Name));
            return null;
        }

        // SDA0144: the generated code calls TConverter.FromDb / .ToDb directly, so both must be
        // present as accessible static methods (implicit interface implementation, not explicit).
        if (!HasCallableStatic(converter, "FromDb") || !HasCallableStatic(converter, "ToDb"))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ConverterStaticAbstractMissing,
                method.Locations.FirstOrDefault(),
                method.Name,
                converter.ToDisplayString()));
            return null;
        }

        // SDA0141: when the converter declares [ConverterSupportedTypes], the CLR (property) type
        // must be in that whitelist.
        if (!IsClrTypeSupported(converter, clrType))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Diagnostics.ConverterTypeNotSupported,
                method.Locations.FirstOrDefault(),
                method.Name,
                memberSymbol.Name));
            return null;
        }

        return new Result(converter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), dbType);
    }

    // All converter types referenced by [TypeHandler]-family attributes on the member, in source
    // order. An entry is null when the attribute's converter type could not be resolved.
    private static List<INamedTypeSymbol?> CollectHandlerConverters(ISymbol memberSymbol)
    {
        var converters = new List<INamedTypeSymbol?>();
        foreach (var attr in memberSymbol.GetAttributes())
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

    private static bool IsClrTypeSupported(INamedTypeSymbol converter, ITypeSymbol clrType)
    {
        var supported = converter.GetAttributes()
            .FirstOrDefault(static a => a.AttributeClass?.ToDisplayString() == ConverterSupportedTypesFq);
        if (supported is null || supported.ConstructorArguments.Length == 0)
        {
            return true;
        }

        var allowed = supported.ConstructorArguments[0].Values;
        if (allowed.IsDefaultOrEmpty)
        {
            return true;
        }

        return allowed.Any(v => v.Value is ITypeSymbol t && SymbolEqualityComparer.Default.Equals(t, clrType));
    }

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
