namespace Smart.Data.Accessor.Shared.Builders.Engine;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Shared.Helpers;

// Resolves the [TypeHandler<TConverter>] scope chain (member (property) → method → accessor class →
// [ExecuteConfig] profile) for Builder entity columns, reusing the shared ConverterScopeHelper
// primitives. Builder applies the result as-is (no converter validation). [TypeMap] / [DbType] /
// [ExecuteConfig] are resolved via the shared MappingAttributeHelper (identical to the core generator).
// Shared source (linked into each builder generator assembly).
internal static class MappingResolver
{
    // resolve the converter for an entity property across member → method → class → profile. The
    // member scope is exclusive (a declared [TypeHandler] governs even when the converter type is
    // unresolved); the outer scopes are type-keyed.
    public static INamedTypeSymbol? ResolveTypeHandler(
        IPropertySymbol property,
        IMethodSymbol method,
        INamedTypeSymbol container,
        INamedTypeSymbol? profile)
    {
        foreach (var attribute in property.GetAttributes())
        {
            if (ConverterScopeHelper.TryGetHandlerConverter(attribute, out var memberConverter))
            {
                return memberConverter;
            }
        }

        return ConverterScopeHelper.FindTypeKeyedConverter(method.GetAttributes(), property.Type)
            ?? ConverterScopeHelper.FindTypeKeyedConverter(container.GetAttributes(), property.Type)
            ?? (profile is null ? null : ConverterScopeHelper.FindTypeKeyedConverter(profile.GetAttributes(), property.Type));
    }
}
