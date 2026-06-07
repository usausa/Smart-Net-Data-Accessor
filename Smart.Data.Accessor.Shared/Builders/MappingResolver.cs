namespace Smart.Data.Accessor.Shared.Builders;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Shared.Helpers;

// [TypeHandler<TConverter>] のスコープ連鎖（member(property) → method → accessor class → [ExecuteConfig] profile）を解決する。
// 共有 ConverterScopeHelper を再利用。Builder は結果をそのまま使う（converter 検証はしない）。
// Resolves the [TypeHandler<TConverter>] scope chain (member (property) → method → accessor class → [ExecuteConfig] profile)
// reusing the shared ConverterScopeHelper primitives. Builder applies the result as-is (no converter validation).
internal static class MappingResolver
{
    // member スコープは排他（宣言された [TypeHandler] が converter 未解決でも支配）、外側スコープは型キー一致。
    // The member scope is exclusive (a declared [TypeHandler] governs even when unresolved); the outer scopes are type-keyed.
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
