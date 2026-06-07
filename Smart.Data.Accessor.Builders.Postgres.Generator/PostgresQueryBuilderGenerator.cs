namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Shared.Builders;

// PostgreSQL QueryBuilder ジェネレータ（配線）。走査は共有 ClassScanner、transform は PostgresModelBuilder、出力は共有 SourceOutput＋PostgresSourceBuilder。
// The PostgreSQL QueryBuilder generator (wiring). Scanning is the shared ClassScanner, the transform is PostgresModelBuilder, output is the shared SourceOutput + PostgresSourceBuilder.
[Generator]
public sealed class PostgresQueryBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellation) => PostgresModelBuilder.Build(context, cancellation))
            .WithTrackingName(ClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (productionContext, model) =>
            SourceOutput.Emit(productionContext, model.Namespace, model.ClassName, model.Accessibility, model.Methods, model.Diagnostics, PostgresSourceBuilder.EmitMethod, ".Postgres"));
    }
}
