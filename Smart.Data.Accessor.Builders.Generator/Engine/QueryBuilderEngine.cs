namespace Smart.Data.Accessor.Builders.Generator.Engine;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Builders.Generator.Builders;
using Smart.Data.Accessor.Builders.Generator.Models;

internal static class QueryBuilderEngine
{
    private const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";

    public enum BuilderKind
    {
        Insert,
        Update,
        Delete,
        Count,
        Select,
        SelectSingle,
        Truncate,
    }

    // 方言でパラメータ化した QueryBuilder 生成エンジンの配線。各プロバイダの [Generator] が自分の QueryBuilder 属性集合と
    // SqlDialect を渡して呼ぶ。登録は [DataAccessor] に対して行うので、1 つの transform がメソッド上の全属性を一度に見られる。
    // transform（BuilderModelBuilder）が等価な BuilderClassModel を作り、出力段（BuilderSourceBuilder）がそのモデルだけ
    // （symbol 非依存）から {Method}__QueryBuilder ヘルパーを生成する＝生成をインクリメンタルに保つ。
    // targets / dialect / providerTag は generator 固定値なのでクロージャで渡す（キャッシュ対象のモデルには含めない）。
    // Wiring of the dialect-parameterized QueryBuilder emit engine. Each provider's [Generator] calls this with its
    // own QueryBuilder attribute set + SqlDialect. Registration is on [DataAccessor], so a single transform sees every
    // attribute on a method at once. The transform (BuilderModelBuilder) produces an equatable BuilderClassModel, and
    // the output stage (BuilderSourceBuilder) emits the {Method}__QueryBuilder helpers purely from that model (no
    // symbols), keeping generation incremental. targets / dialect / providerTag are generator-fixed values passed via
    // closures (never part of the cached model).
    public static void Register(
        IncrementalGeneratorInitializationContext context,
        IReadOnlyList<(string Attribute, BuilderKind Kind)> targets,
        SqlDialect dialect,
        string providerTag)
    {
        var models = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                (ctx, ct) => BuilderModelBuilder.Build(ctx, targets, ct))
            .WithTrackingName("BuilderClassModel");

        context.RegisterSourceOutput(models, (spc, model) => Emit(spc, model, dialect, providerTag));
    }

    // 1 クラス分の出力：まず診断を報告し、QueryBuilder メソッドが 1 つも無ければ何も出力しない。あれば
    // BuilderSourceBuilder で方言に応じたヘルパーソースを生成し、{ns}_{Class}.QueryBuilders{providerTag}.g.cs として追加する。
    // Emit one class: report diagnostics first; if there are no QueryBuilder methods, emit nothing. Otherwise generate
    // the dialect-specific helper source via BuilderSourceBuilder and add it as {ns}_{Class}.QueryBuilders{providerTag}.g.cs.
    private static void Emit(SourceProductionContext context, BuilderClassModel model, SqlDialect dialect, string providerTag)
    {
        foreach (var diagnostic in model.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }

        if (model.Methods.Count == 0)
        {
            return;
        }

        var source = BuilderSourceBuilder.Build(model, dialect);
        var nsForFile = String.IsNullOrEmpty(model.Namespace) ? "global" : model.Namespace.Replace('.', '_');
        var filename = $"{nsForFile}_{model.ClassName}.QueryBuilders{providerTag}.g.cs";
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    }
}
