namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.Postgres.Generator.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;

// PostgreSQL QueryBuilder ジェネレータ（配線）。[PgInsert]/…/[PgTruncate]/[PgUpsert] が付いたメソッドに {Method}__QueryBuilder
// ヘルパーを生成する（二重引用符、LIMIT/OFFSET、ON CONFLICT、RETURNING）。走査は共有 BuilderClassScanner、transform は
// PostgresModelBuilder、出力は共有 BuilderOutput＋PostgresSourceBuilder に委譲する（3 層）。
// The PostgreSQL QueryBuilder generator (wiring). Emits the {Method}__QueryBuilder helper for methods carrying the
// [PgInsert]/…/[PgTruncate]/[PgUpsert] attributes (double-quote quoting, LIMIT/OFFSET, ON CONFLICT, RETURNING). Scanning
// is the shared BuilderClassScanner, the transform is PostgresModelBuilder, output is the shared BuilderOutput + PostgresSourceBuilder.
[Generator]
public sealed class PostgresQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.Pg";

    private static readonly (string Attribute, PostgresOperation Operation)[] Targets =
    [
        (Ns + "InsertAttribute", PostgresOperation.Insert),
        (Ns + "UpdateAttribute", PostgresOperation.Update),
        (Ns + "DeleteAttribute", PostgresOperation.Delete),
        (Ns + "CountAttribute", PostgresOperation.Count),
        (Ns + "SelectAttribute", PostgresOperation.Select),
        (Ns + "SelectSingleAttribute", PostgresOperation.SelectSingle),
        (Ns + "TruncateAttribute", PostgresOperation.Truncate),
        (Ns + "UpsertAttribute", PostgresOperation.Upsert)
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BuilderClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => BuilderClassScanner.Scan(ctx, Targets, PostgresModelBuilder.BuildMethod, ct))
            .WithTrackingName(BuilderClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (spc, model) => BuilderOutput.Emit(spc, model, PostgresSourceBuilder.EmitMethod, ".Postgres"));
    }
}
