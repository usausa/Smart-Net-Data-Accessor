namespace Smart.Data.Accessor.Generator.Builders;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// v1 skeleton (spec.md §7.11.2). Filled in by Phase 6.3.
internal static class EntityModelBuilder
{
    public static Result<EntityModel> Build(ITypeSymbol entityTypeSymbol)
    {
        _ = entityTypeSymbol;
        var placeholderColumn = new ColumnModel(
            string.Empty,
            string.Empty,
            string.Empty,
            false,
            false,
            false,
            false,
            null,
            null,
            null,
            null);
        var placeholderConverter = new ConverterBindingModel(string.Empty, string.Empty, string.Empty);
        var placeholder = new EntityModel(string.Empty, EntityCtorKind.Parameterless, EquatableArray<ColumnModel>.Empty);
        _ = placeholderColumn;
        _ = placeholderConverter;
        _ = placeholder;
        return Results.Error<EntityModel>(null);
    }
}
