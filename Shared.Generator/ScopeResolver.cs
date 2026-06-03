namespace Smart.Data.Accessor.CodeGen;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

// 改善2 ②: shared (linked source) TypeHandler resolution primitives used by both generators — pure
// symbol→symbol/type functions. The scope-chain *ordering* (member→method→class→profile) and the
// core's converter *validation* (SDA0142-0145) stay per-generator; only the duplicated building
// blocks live here. Compiled `internal` into each generator assembly (no DLL; §1.4.4 と両立).
internal static class ScopeResolver
{
    public const string TypeHandlerGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute<TConverter>";
    public const string TypeHandlerNonGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute";
    public const string IValueConverterFq = "Smart.Data.Accessor.Converters.IValueConverter<TDb, TClr>";

    // The TConverter of a [TypeHandler]-family attribute (null when the type could not be resolved);
    // false when the attribute is not a [TypeHandler] family member.
    public static bool TryGetHandlerConverter(AttributeData attr, out INamedTypeSymbol? converter)
    {
        for (var current = attr.AttributeClass; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType && current.ConstructedFrom.ToDisplayString() == TypeHandlerGenericFq)
            {
                converter = current.TypeArguments[0] as INamedTypeSymbol;
                return true;
            }
            if (current.ToDisplayString() == TypeHandlerNonGenericFq)
            {
                converter = attr.ConstructorArguments.Length > 0
                    ? attr.ConstructorArguments[0].Value as INamedTypeSymbol
                    : null;
                return true;
            }
        }
        converter = null;
        return false;
    }

    // All [TypeHandler]-family converters in source order (an entry is null when its type is unresolved).
    public static List<INamedTypeSymbol?> CollectHandlerConverters(ImmutableArray<AttributeData> attributes)
    {
        var converters = new List<INamedTypeSymbol?>();
        foreach (var attr in attributes)
        {
            if (TryGetHandlerConverter(attr, out var converter))
            {
                converters.Add(converter);
            }
        }
        return converters;
    }

    // The first [TypeHandler] at this scope whose IValueConverter TClr matches valueType (Nullable<T>
    // compares against T). Non-matching handlers apply to other types and are skipped.
    public static INamedTypeSymbol? FindTypeKeyedConverter(ImmutableArray<AttributeData> attributes, ITypeSymbol valueType)
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

    // The IValueConverter<TDb, TClr> type arguments of a converter; false when it does not implement it.
    public static bool TryGetConverterTypes(INamedTypeSymbol converter, out ITypeSymbol dbType, out ITypeSymbol clrType)
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

    public static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol nt && nt.IsGenericType &&
        nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            ? nt.TypeArguments[0]
            : type;
}
