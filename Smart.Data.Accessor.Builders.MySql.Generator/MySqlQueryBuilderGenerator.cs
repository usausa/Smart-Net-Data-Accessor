namespace Smart.Data.Accessor.Builders.MySql.Generator;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.GeneratorShared.Engine;
using Smart.Data.Accessor.Builders.MySql.Generator.Models;

// MySQL QueryBuilder ジェネレータ（配線）。[MySqlInsert]/…/[MySqlInsertIgnore] が付いたメソッドに {Method}__QueryBuilder ヘルパーを
// 生成する（バッククォート、LIMIT/OFFSET、ON DUPLICATE KEY UPDATE / REPLACE / INSERT IGNORE）。走査は共有 BuilderClassScanner、
// transform は MySqlModelBuilder、出力は共有 BuilderOutput＋MySqlSourceBuilder に委譲する（3 層）。
// The MySQL QueryBuilder generator (wiring). Emits the {Method}__QueryBuilder helper for methods carrying the
// [MySqlInsert]/…/[MySqlInsertIgnore] attributes (backtick quoting, LIMIT/OFFSET, ON DUPLICATE KEY UPDATE / REPLACE /
// INSERT IGNORE). Scanning is the shared BuilderClassScanner, the transform is MySqlModelBuilder, output is the shared
// BuilderOutput + MySqlSourceBuilder.
[Generator]
public sealed class MySqlQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.MySql";

    private static readonly (string Attribute, MySqlOperation Operation)[] Targets =
    [
        (Ns + "InsertAttribute", MySqlOperation.Insert),
        (Ns + "UpdateAttribute", MySqlOperation.Update),
        (Ns + "DeleteAttribute", MySqlOperation.Delete),
        (Ns + "CountAttribute", MySqlOperation.Count),
        (Ns + "SelectAttribute", MySqlOperation.Select),
        (Ns + "SelectSingleAttribute", MySqlOperation.SelectSingle),
        (Ns + "TruncateAttribute", MySqlOperation.Truncate),
        (Ns + "UpsertAttribute", MySqlOperation.Upsert),
        (Ns + "ReplaceAttribute", MySqlOperation.Replace),
        (Ns + "InsertIgnoreAttribute", MySqlOperation.InsertIgnore),
    ];

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BuilderClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => BuilderClassScanner.Scan(ctx, Targets, MySqlModelBuilder.BuildMethod, ct))
            .WithTrackingName(BuilderClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (spc, model) => BuilderOutput.Emit(spc, model, MySqlSourceBuilder.EmitMethod, ".MySql"));
    }
}
