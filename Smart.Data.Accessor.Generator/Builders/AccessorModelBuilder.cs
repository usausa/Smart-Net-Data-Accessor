namespace Smart.Data.Accessor.Generator.Builders;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// v1 skeleton (spec.md §7.11.2). Filled in by Phase 6.3.
internal static class AccessorModelBuilder
{
    public static Result<AccessorModel> Build(
        INamedTypeSymbol accessorSymbol,
        Compilation compilation)
    {
        _ = accessorSymbol;
        _ = compilation;
        _ = CreatePlaceholder();
        return Results.Error<AccessorModel>(null);
    }

    private static AccessorModel CreatePlaceholder()
    {
        var inject = new InjectModel(string.Empty, string.Empty);
        var typeMap = new TypeMapModel(string.Empty, string.Empty);
        var typeHandler = new TypeHandlerModel(string.Empty, string.Empty);
        var parameter = new ParameterModel(string.Empty, string.Empty, false, false, null, null);
        var ret = new ReturnModel(string.Empty, null, null);
        var method = new MethodModel(
            string.Empty,
            Models.MethodKind.Execute,
            Models.ConnectionMode.PatternA,
            '@',
            null,
            null,
            false,
            false,
            null,
            new EquatableArray<ParameterModel>([parameter]),
            ret,
            new EquatableArray<TypeHandlerModel>([typeHandler]));
        return new AccessorModel(
            string.Empty,
            string.Empty,
            null,
            null,
            null,
            new EquatableArray<InjectModel>([inject]),
            false,
            new EquatableArray<MethodModel>([method]),
            new EquatableArray<TypeMapModel>([typeMap]),
            new EquatableArray<TypeHandlerModel>([typeHandler]));
    }
}
