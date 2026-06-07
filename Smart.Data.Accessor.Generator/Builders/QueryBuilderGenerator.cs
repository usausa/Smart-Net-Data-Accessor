namespace Smart.Data.Accessor.Generator.Builders;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Shared.Builders;

// 標準（既定）QueryBuilder ジェネレータ（配線）。コアの [Insert]/[Update]/[Delete]/[Count]/[Select]/[SelectSingle]/[Truncate] が
// 付いたメソッドに {Method}__QueryBuilder ヘルパーを生成する。走査は共有 ClassScanner、transform は StandardModelBuilder、出力は
// 共有 SourceOutput＋StandardSourceBuilder に委譲する（3 層）。他プロバイダーも同形。
// The standard (default) QueryBuilder generator (wiring). Emits the {Method}__QueryBuilder helper for methods carrying the
// core [Insert]/…/[Truncate] attributes. Scanning is the shared ClassScanner, the transform is StandardModelBuilder, output
// is the shared SourceOutput + StandardSourceBuilder (the 3 layers). Each provider follows the same shape.
[Generator]
public sealed class QueryBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellation) => StandardModelBuilder.Build(context, cancellation))
            .WithTrackingName(ClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (productionContext, model) =>
            SourceOutput.Emit(productionContext, model.Namespace, model.ClassName, model.Accessibility, model.Methods, model.Diagnostics, StandardSourceBuilder.EmitMethod, string.Empty));
    }
}
