namespace Smart.Data.Accessor.Builders.Generator.Engine;

using System.Collections.Generic;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.CodeGen;

/// <summary>
/// Resolves <c>[TypeHandler&lt;TConverter&gt;]</c> / <c>[TypeMap]</c> / <c>[DbType]</c> for Builder
/// parameter emission (spec §7.4 / §7.7). The TypeHandler scope chain (member (property) → method →
/// accessor class → <c>[ExecuteConfig]</c> profile) reuses the shared <see cref="ScopeResolver"/>
/// primitives (改善2 ②); Builder applies the resolution as-is (no converter validation = 2-C).
/// Shared source (linked into each builder generator assembly).
/// </summary>
internal static class MappingResolver
{
    private const string TypeMapAttributeFq = "Smart.Data.Accessor.Attributes.TypeMapAttribute";
    private const string DbTypeAttributeFq = "Smart.Data.Accessor.Attributes.DbTypeAttribute";
    private const string ExecuteConfigAttributeFq = "Smart.Data.Accessor.Attributes.ExecuteConfigAttribute";

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
            if (ScopeResolver.TryGetHandlerConverter(attr, out var memberConverter))
            {
                return memberConverter;
            }
        }

        return ScopeResolver.FindTypeKeyedConverter(method.GetAttributes(), prop.Type)
            ?? ScopeResolver.FindTypeKeyedConverter(container.GetAttributes(), prop.Type)
            ?? (profile is null ? null : ScopeResolver.FindTypeKeyedConverter(profile.GetAttributes(), prop.Type));
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
