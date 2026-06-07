namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Shared.Builders;

// SQL Server QueryBuilder ジェネレータ（配線）。[SqlInsert]/…/[SqlTruncate]/[SqlMerge] が付いたメソッドに {Method}__QueryBuilder
// ヘルパーを生成する（角括弧クォート、OFFSET/FETCH ページング、MERGE/OUTPUT）。走査は共有 ClassScanner、transform は
// SqlServerModelBuilder、出力は共有 SourceOutput＋SqlServerSourceBuilder に委譲する（3 層）。
// The SQL Server QueryBuilder generator (wiring). Scanning is the shared ClassScanner, the transform is SqlServerModelBuilder,
// output is the shared SourceOutput + SqlServerSourceBuilder.
[Generator]
public sealed class SqlServerQueryBuilderGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                ClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (context, cancellation) => SqlServerModelBuilder.Build(context, cancellation))
            .WithTrackingName(ClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (productionContext, model) =>
            SourceOutput.Emit(productionContext, model.Namespace, model.ClassName, model.Accessibility, model.Methods, model.Diagnostics, SqlServerSourceBuilder.EmitMethod, ".SqlServer"));
    }
}
