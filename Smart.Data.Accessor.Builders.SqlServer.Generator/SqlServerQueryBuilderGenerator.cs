namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Smart.Data.Accessor.Builders.SqlServer.Generator.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;

// SQL Server QueryBuilder ジェネレータ（配線）。[SqlInsert]/…/[SqlTruncate]/[SqlMerge] が付いたメソッドに {Method}__QueryBuilder
// ヘルパーを生成する（角括弧クォート、OFFSET/FETCH ページング、MERGE/OUTPUT）。走査は共有 BuilderClassScanner、transform は
// SqlServerModelBuilder、出力は共有 BuilderOutput＋SqlServerSourceBuilder に委譲する（3 層）。
// The SQL Server QueryBuilder generator (wiring). Emits the {Method}__QueryBuilder helper for methods carrying the
// [SqlInsert]/…/[SqlTruncate]/[SqlMerge] attributes (bracket quoting, OFFSET/FETCH paging, MERGE/OUTPUT). Scanning is the
// shared BuilderClassScanner, the transform is SqlServerModelBuilder, output is the shared BuilderOutput + SqlServerSourceBuilder.
[Generator]
public sealed class SqlServerQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.Sql";

    private static readonly (string Attribute, SqlServerOperation Operation)[] Targets =
    [
        (Ns + "InsertAttribute", SqlServerOperation.Insert),
        (Ns + "UpdateAttribute", SqlServerOperation.Update),
        (Ns + "DeleteAttribute", SqlServerOperation.Delete),
        (Ns + "CountAttribute", SqlServerOperation.Count),
        (Ns + "SelectAttribute", SqlServerOperation.Select),
        (Ns + "SelectSingleAttribute", SqlServerOperation.SelectSingle),
        (Ns + "TruncateAttribute", SqlServerOperation.Truncate),
        (Ns + "MergeAttribute", SqlServerOperation.Merge)
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BuilderClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => BuilderClassScanner.Scan(ctx, Targets, SqlServerModelBuilder.BuildMethod, ct))
            .WithTrackingName(BuilderClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (spc, model) => BuilderOutput.Emit(spc, model, SqlServerSourceBuilder.EmitMethod, ".SqlServer"));
    }
}
