namespace Smart.Data.Accessor.Builders.Generator.Builders;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// v1 skeleton (spec.md §7.11.2). Filled in by Phase 5.x.
internal static class BuilderEntityModelBuilder
{
    public static Result<BuilderEntityModel> Build(ITypeSymbol entityTypeSymbol)
    {
        _ = entityTypeSymbol;
        _ = CreatePlaceholder();
        return Results.Error<BuilderEntityModel>(null);
    }

    private static BuilderEntityModel CreatePlaceholder()
    {
        var column = new BuilderColumnModel(
            string.Empty,
            string.Empty,
            string.Empty,
            false,
            false,
            false,
            null);
        return new BuilderEntityModel(
            string.Empty,
            new EquatableArray<BuilderColumnModel>([column]));
    }
}
