namespace Smart.Data.Accessor.Generator.Builders;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Smart.Data.Accessor.Generator.Builders.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;

// 標準（既定）QueryBuilder ジェネレータ（配線）。コアの [Insert]/[Update]/[Delete]/[Count]/[Select]/[SelectSingle]/[Truncate] が
// 付いたメソッドに {Method}__QueryBuilder ヘルパーを生成する。[DataAccessor] で登録し、走査は共有 BuilderClassScanner、transform は
// StandardModelBuilder、出力は共有 BuilderOutput＋StandardSourceBuilder に委譲する（3 層）。他プロバイダーも同形。
// The standard (default) QueryBuilder generator (wiring). Emits the {Method}__QueryBuilder helper for methods carrying
// the core [Insert]/…/[Truncate] attributes. Registers on [DataAccessor]; scanning is the shared BuilderClassScanner,
// the transform is StandardModelBuilder, output is the shared BuilderOutput + StandardSourceBuilder (the 3 layers). Each
// provider follows the same shape.
[Generator]
public sealed class QueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.";

    private static readonly (string Attribute, Operation Operation)[] Targets =
    [
        (Ns + "InsertAttribute", Operation.Insert),
        (Ns + "UpdateAttribute", Operation.Update),
        (Ns + "DeleteAttribute", Operation.Delete),
        (Ns + "CountAttribute", Operation.Count),
        (Ns + "SelectAttribute", Operation.Select),
        (Ns + "SelectSingleAttribute", Operation.SelectSingle),
        (Ns + "TruncateAttribute", Operation.Truncate)
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BuilderClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => BuilderClassScanner.Scan(ctx, Targets, StandardModelBuilder.BuildMethod, ct))
            .WithTrackingName(BuilderClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (spc, model) => BuilderOutput.Emit(spc, model, StandardSourceBuilder.EmitMethod, string.Empty));
    }
}
