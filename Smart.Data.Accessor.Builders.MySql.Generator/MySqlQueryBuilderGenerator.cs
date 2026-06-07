namespace Smart.Data.Accessor.Builders.MySql.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Shared.Builders;

// MySQL QueryBuilder ジェネレータ（配線）。走査は共有 ClassScanner、transform は MySqlModelBuilder、出力は共有 SourceOutput＋MySqlSourceBuilder。
// The MySQL QueryBuilder generator (wiring). Scanning is the shared ClassScanner, the transform is MySqlModelBuilder, output is the shared SourceOutput + MySqlSourceBuilder.
[Generator]
public sealed class MySqlQueryBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellation) => MySqlModelBuilder.Build(context, cancellation))
            .WithTrackingName(ClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (productionContext, model) =>
            SourceOutput.Emit(productionContext, model.Namespace, model.ClassName, model.Accessibility, model.Methods, model.Diagnostics, MySqlSourceBuilder.EmitMethod, ".MySql"));
    }
}
