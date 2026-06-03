namespace Smart.Data.Accessor.Builders.Generator.Engine;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

/// <summary>
/// Resolves <c>[TypeHandler&lt;TConverter&gt;]</c> / <c>[TypeMap]</c> / <c>[DbType]</c> for Builder
/// parameter emission (spec §7.4 / §7.7). Mirrors the core <c>ConverterResolver</c> scope chain:
/// member (property) → method → accessor class → <c>[ExecuteConfig]</c> profile. The member scope is
/// an explicit binding; the outer scopes are type-keyed (a handler applies only when its TClr matches
/// the property type). Shared source (linked into each builder generator assembly).
/// </summary>
internal static class MappingResolver
{
    private const string TypeHandlerGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute<TConverter>";
    private const string TypeHandlerNonGenericFq = "Smart.Data.Accessor.Attributes.TypeHandlerAttribute";
    private const string TypeMapAttributeFq = "Smart.Data.Accessor.Attributes.TypeMapAttribute";
    private const string DbTypeAttributeFq = "Smart.Data.Accessor.Attributes.DbTypeAttribute";
    private const string ExecuteConfigAttributeFq = "Smart.Data.Accessor.Attributes.ExecuteConfigAttribute";
    private const string IValueConverterFq = "Smart.Data.Accessor.Converters.IValueConverter<TDb, TClr>";

    // spec §7.7: resolve the converter for an entity property across member → method → class →
    // profile. The member scope is exclusive (a declared [TypeHandler] governs even when the
    // converter type is unresolved); the outer scopes are type-keyed.
    public static INamedTypeSymbol? ResolveTypeHandler(
        IPropertySymbol prop,
        IMethodSymbol method,
        INamedTypeSymbol container,
        INamedTypeSymbol? profile)
    {
        foreach (var attr in prop.GetAttributes())
        {
            if (TryGetHandlerConverter(attr, out var memberConverter))
            {
                return memberConverter;
            }
        }

        return FindTypeKeyedHandler(method.GetAttributes(), prop.Type)
            ?? FindTypeKeyedHandler(container.GetAttributes(), prop.Type)
            ?? (profile is null ? null : FindTypeKeyedHandler(profile.GetAttributes(), prop.Type));
    }

    // spec §7.7: the type referenced by [ExecuteConfig(typeof(P))] on the accessor (null when absent).
    public static INamedTypeSymbol? ResolveProfile(INamedTypeSymbol container)
    {
        foreach (var attr in container.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == ExecuteConfigAttributeFq &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is INamedTypeSymbol profile)
            {
                return profile;
            }
        }
        return null;
    }

    // spec §5.4 (F3): the DbType expression from a property-scope [DbType(DbType)] (non-generic), or
    // null. Lets entity-mode INSERT/UPDATE set p.DbType per column; takes precedence over [TypeMap].
    public static string? ResolvePropertyDbType(IPropertySymbol prop)
    {
        foreach (var attr in prop.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() == DbTypeAttributeFq &&
                attr.ConstructorArguments.Length > 0 &&
                attr.ConstructorArguments[0].Value is int dbTypeRaw)
            {
                return $"(global::System.Data.DbType){dbTypeRaw}";
            }
        }
        return null;
    }

    // True when the attribute is a [TypeHandler]-family attribute; converter is its TConverter
    // (null when the type could not be resolved).
    private static bool TryGetHandlerConverter(AttributeData attr, out INamedTypeSymbol? converter)
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

    // The first [TypeHandler] at this scope whose IValueConverter TClr matches the property type
    // (Nullable<T> compares against T). Non-matching handlers apply to other types and are skipped.
    private static INamedTypeSymbol? FindTypeKeyedHandler(ImmutableArray<AttributeData> attributes, ITypeSymbol clrType)
    {
        var underlying = UnwrapNullable(clrType);
        foreach (var attr in attributes)
        {
            if (TryGetHandlerConverter(attr, out var converter) &&
                converter is not null &&
                TryGetConverterClr(converter, out var handlerClr) &&
                SymbolEqualityComparer.Default.Equals(handlerClr, underlying))
            {
                return converter;
            }
        }
        return null;
    }

    private static bool TryGetConverterClr(INamedTypeSymbol converter, out ITypeSymbol clrType)
    {
        var iface = converter.AllInterfaces.FirstOrDefault(static i =>
            i.IsGenericType && i.ConstructedFrom.ToDisplayString() == IValueConverterFq);
        if (iface is null)
        {
            clrType = null!;
            return false;
        }
        clrType = iface.TypeArguments[1];
        return true;
    }

    // spec §7.7 (P8): the converter's IValueConverter<TDb, TClr> type-argument FQNs, for emitting
    // ExecuteHelper.AddInParameter<TConverter, TDb, TClr>. False when the type does not implement it.
    public static bool TryGetConverterTypes(INamedTypeSymbol converter, out string dbTypeFullName, out string clrTypeFullName)
    {
        var iface = converter.AllInterfaces.FirstOrDefault(static i =>
            i.IsGenericType && i.ConstructedFrom.ToDisplayString() == IValueConverterFq);
        if (iface is null)
        {
            dbTypeFullName = string.Empty;
            clrTypeFullName = string.Empty;
            return false;
        }
        dbTypeFullName = iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        clrTypeFullName = iface.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return true;
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type) =>
        type is INamedTypeSymbol nt && nt.IsGenericType &&
        nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T
            ? nt.TypeArguments[0]
            : type;

    public static Dictionary<ITypeSymbol, TypeMapInfo> ReadTypeMaps(INamedTypeSymbol container)
    {
        var map = new Dictionary<ITypeSymbol, TypeMapInfo>(SymbolEqualityComparer.Default);
        foreach (var attr in container.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != TypeMapAttributeFq)
            {
                continue;
            }
            if (attr.ConstructorArguments.Length < 2)
            {
                continue;
            }
            if (attr.ConstructorArguments[0].Value is not INamedTypeSymbol clrType)
            {
                continue;
            }
            if (attr.ConstructorArguments[1].Value is not int dbTypeRaw)
            {
                continue;
            }
            int? size = null;
            foreach (var kv in attr.NamedArguments)
            {
                if (kv.Key == "Size" && kv.Value.Value is int sz)
                {
                    size = sz;
                }
            }
            map[clrType] = new TypeMapInfo($"(global::System.Data.DbType){dbTypeRaw}", size);
        }
        return map;
    }

    public static bool HasTypeMapFor(ITypeSymbol propertyType, Dictionary<ITypeSymbol, TypeMapInfo> typeMaps)
    {
        if (typeMaps.ContainsKey(propertyType))
        {
            return true;
        }
        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return typeMaps.ContainsKey(nt.TypeArguments[0]);
        }
        return false;
    }

    public static bool TryGetTypeMap(
        ITypeSymbol propertyType,
        Dictionary<ITypeSymbol, TypeMapInfo> typeMaps,
        out TypeMapInfo info)
    {
        if (typeMaps.TryGetValue(propertyType, out info))
        {
            return true;
        }
        if (propertyType is INamedTypeSymbol nt && nt.IsGenericType &&
            nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T &&
            typeMaps.TryGetValue(nt.TypeArguments[0], out info))
        {
            return true;
        }
        info = default;
        return false;
    }
}

internal readonly record struct TypeMapInfo(string DbTypeExpr, int? Size);
