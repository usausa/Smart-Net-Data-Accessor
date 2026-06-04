namespace Smart.Data.Accessor.GeneratorShared;

using System.Collections.Immutable;

using Microsoft.CodeAnalysis;

// Shared (linked source) resolution + construction for the standard [TypeMap] / [DbType] /
// [ExecuteConfig] attributes — used identically by the core and Builder generators so a Builder
// method honours class+profile [TypeMap], property/parameter [DbType], and [ExecuteConfig] profiles
// exactly like a core method. Pure symbol→value/Model functions; the equatable Model TypeMapInfo lives
// here too (a standard-attribute resolution result, not an entity Mapping Model — it carries no
// generator-specific / Builder-specific shape).
internal static class MappingAttributeHelper
{
    private const string TypeMapAttributeFq = "Smart.Data.Accessor.Attributes.TypeMapAttribute";
    private const string DbTypeAttributeFq = "Smart.Data.Accessor.Attributes.DbTypeAttribute";
    private const string ExecuteConfigAttributeFq = "Smart.Data.Accessor.Attributes.ExecuteConfigAttribute";

    // The profile referenced by [ExecuteConfig(typeof(P))] on the accessor (null when absent).
    public static INamedTypeSymbol? ResolveProfile(INamedTypeSymbol container)
    {
        foreach (var attr in container.GetAttributes())
        {
            if ((attr.AttributeClass?.ToDisplayString() == ExecuteConfigAttributeFq) &&
                (attr.ConstructorArguments.Length >= 1) &&
                (attr.ConstructorArguments[0].Value is INamedTypeSymbol profile))
            {
                return profile;
            }
        }
        return null;
    }

    // Class + profile [TypeMap] lookup (unwrapped CLR type FQN → TypeMapInfo). Class scope is collected
    // first so it wins over the profile (first occurrence wins).
    public static Dictionary<string, TypeMapInfo> BuildTypeMapLookup(INamedTypeSymbol container, INamedTypeSymbol? profile)
    {
        var map = new Dictionary<string, TypeMapInfo>(StringComparer.Ordinal);
        CollectTypeMaps(container, map);
        if (profile is not null)
        {
            CollectTypeMaps(profile, map);
        }
        return map;
    }

    private static void CollectTypeMaps(INamedTypeSymbol owner, Dictionary<string, TypeMapInfo> map)
    {
        foreach (var attr in owner.GetAttributes())
        {
            if ((attr.AttributeClass?.ToDisplayString() != TypeMapAttributeFq) ||
                (attr.ConstructorArguments.Length < 2) ||
                (attr.ConstructorArguments[0].Value is not ITypeSymbol clrType) ||
                (attr.ConstructorArguments[1].Value is not int dbTypeValue))
            {
                continue;
            }

            var key = ConverterScopeHelper.UnwrapNullable(clrType).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (map.ContainsKey(key))
            {
                continue;   // first occurrence wins (class collected before profile)
            }

            int? size = null;
            foreach (var na in attr.NamedArguments)
            {
                if ((na.Key == "Size") && (na.Value.Value is int s))
                {
                    size = s;
                }
            }
            map[key] = new TypeMapInfo($"(global::System.Data.DbType){dbTypeValue}", size);
        }
    }

    // Looks up the [TypeMap] default for a value type (Nullable<T> falls back to T).
    public static bool TryGetTypeMap(ITypeSymbol type, Dictionary<string, TypeMapInfo> lookup, out TypeMapInfo info)
    {
        var key = ConverterScopeHelper.UnwrapNullable(type).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return lookup.TryGetValue(key, out info);
    }

    // A property-scope [DbType(DbType)] expression (non-generic), or null.
    public static string? ResolvePropertyDbType(IPropertySymbol prop) => ResolveDbType(prop.GetAttributes());

    // A parameter-scope [DbType(DbType)] expression (non-generic), or null.
    public static string? ResolveParameterDbType(IParameterSymbol parameter) => ResolveDbType(parameter.GetAttributes());

    private static string? ResolveDbType(ImmutableArray<AttributeData> attributes)
    {
        foreach (var attr in attributes)
        {
            if ((attr.AttributeClass?.ToDisplayString() == DbTypeAttributeFq) &&
                (attr.ConstructorArguments.Length > 0) &&
                (attr.ConstructorArguments[0].Value is int dbTypeRaw))
            {
                return $"(global::System.Data.DbType){dbTypeRaw}";
            }
        }
        return null;
    }
}

// Equatable resolution result of a [TypeMap] entry (shared by both generators).
internal readonly record struct TypeMapInfo(string DbTypeExpr, int? Size);
