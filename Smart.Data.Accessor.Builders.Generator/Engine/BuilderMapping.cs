namespace Smart.Data.Accessor.Builders.Generator.Engine;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.GeneratorShared;

/// <summary>
/// Resolves the <c>[TypeHandler&lt;TConverter&gt;]</c> scope chain (member (property) → method →
/// accessor class → <c>[ExecuteConfig]</c> profile) for Builder entity columns, reusing the shared
/// <see cref="ConverterScopeHelper"/> primitives (改善2 ②). Builder applies the result as-is (no converter
/// validation = 2-C). <c>[TypeMap]</c> / <c>[DbType]</c> / <c>[ExecuteConfig]</c> are resolved via the
/// shared <see cref="MappingAttributeHelper"/> (identical to the core generator). Shared source (linked
/// into each builder generator assembly).
/// </summary>
internal static class MappingResolver
{
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
            if (ConverterScopeHelper.TryGetHandlerConverter(attr, out var memberConverter))
            {
                return memberConverter;
            }
        }

        return ConverterScopeHelper.FindTypeKeyedConverter(method.GetAttributes(), prop.Type)
            ?? ConverterScopeHelper.FindTypeKeyedConverter(container.GetAttributes(), prop.Type)
            ?? (profile is null ? null : ConverterScopeHelper.FindTypeKeyedConverter(profile.GetAttributes(), prop.Type));
    }
}
