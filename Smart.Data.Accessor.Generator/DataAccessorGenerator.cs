namespace Smart.Data.Accessor.Generator;

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

[Generator]
public sealed class DataAccessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // === インクリメンタル生成パイプラインの配線（本体は配線のみで、実処理は 2 層に分離している）===
        //   AccessorModelBuilder … transform 段：symbol を等価な Result<AccessorModel>（Model ＋ 診断）へ変換
        //   AccessorSourceBuilder … emit 段：Model を生成 C# 文字列へ。symbol 非依存なので単体テスト可能
        // === Wiring of the incremental generation pipeline (this type only wires; the work is split in two) ===
        //   AccessorModelBuilder  — transform stage: symbol -> equatable Result<AccessorModel> (model + diagnostics)
        //   AccessorSourceBuilder — emit stage: Model -> generated C# string; symbol-free, so unit-testable

        // SQL フォルダ名はプロジェクト毎に MSBuild プロパティ <SmartDataAccessor_SqlFolder> で変更できる（既定 "Sql"）。
        // .targets が CompilerVisibleProperty として公開し、ここでは AnalyzerConfigOptions から読み取る。
        // The SQL folder name is configurable per project via the <SmartDataAccessor_SqlFolder> MSBuild property
        // (default "Sql"); the .targets exposes it as a CompilerVisibleProperty, read here from AnalyzerConfigOptions.
        var sqlFolder = context.AnalyzerConfigOptionsProvider.Select(static (x, _) =>
            x.GlobalOptions.TryGetValue("build_property.SmartDataAccessor_SqlFolder", out var folder) &&
                !String.IsNullOrWhiteSpace(folder)
                ? folder
                : "Sql");

        // 追加ファイル（AdditionalText）から .sql を集め、(ファイル名, 本文) に射影し、SQL フォルダ名と結合する。
        // Collect .sql additional files, project each to (file name, text), and combine them with the SQL folder name.
        var sqlFiles = context.AdditionalTextsProvider
            .Where(static x => x.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(static (t, cancellation) => (
                FullPath: t.Path,
                Path: Path.GetFileNameWithoutExtension(t.Path),
                Text: t.GetText(cancellation)?.ToString() ?? string.Empty))
            .Combine(sqlFolder)
            .Where(static pair =>
            {
                // 親ディレクトリ名が {SqlFolder} に一致する .sql だけを対象にする（無関係な .sql を除外）。
                // Keep only .sql whose parent directory name matches {SqlFolder} (excludes unrelated .sql files).
                var parentDir = Path.GetFileName(Path.GetDirectoryName(pair.Left.FullPath));
                return String.Equals(parentDir, pair.Right, StringComparison.OrdinalIgnoreCase);
            })
            .Select(static (pair, _) => (pair.Left.Path, pair.Left.Text))
            .Collect();

        // [DataAccessor] クラスを FAWMN で拾い、transform 段で等価な Result<AccessorModel>（Model ＋ 診断）へ変換する。
        // ここがインクリメンタルキャッシュの境界。SQL の解決・解析は .sql を要するので後段（出力段）に置き、
        // Compilation 非依存に保つことでキャッシュを効かせる。
        // Pick up [DataAccessor] classes via FAWMN and convert each, in the transform stage, into an equatable
        // Result<AccessorModel> (model + diagnostics) — the incremental cache boundary. SQL resolution/parsing
        // needs the .sql files, so it runs in a later (output) stage and is kept Compilation-free so it caches.
        var classResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AccessorModelBuilder.DataAccessorAttributeName,
                static (x, _) => x is ClassDeclarationSyntax,
                static (context, _) => AccessorModelBuilder.BuildClassResult(context))
            .WithTrackingName("AccessorClassResult");

        // 各アクセサの暫定 Model と .sql 集合を結合し、SQL を解決・解析して Model を完成させる。
        // Combine each accessor's partial model with the .sql set, then resolve/parse SQL to complete the model.
        var completed = classResults
            .Combine(sqlFiles)
            .Select(static (pair, cancellation) => AccessorModelBuilder.CompleteModel(pair.Left, pair.Right, cancellation))
            .WithTrackingName("AccessorCompleted");

        // アクセサ毎のソース＋診断を出力する。completed は [DataAccessor] クラス 1 個につき 1 要素のストリームなので、
        // RegisterSourceOutput は要素（＝クラス）毎に 1 回走り、そのクラスの {ns}_{Class}.g.cs を出力する。
        // 自分の Result<AccessorModel> が変わったアクセサだけ再生成され、変化のないものはキャッシュされ skip される（クラス単位の粒度）。
        // Emit per-accessor source + diagnostics. `completed` is a stream with one element per [DataAccessor]
        // class, so RegisterSourceOutput runs once per element (= per class), emitting that class's
        // {ns}_{Class}.g.cs. Only accessors whose own Result<AccessorModel> changed are re-emitted; unchanged
        // ones stay cached and are skipped (per-class granularity).
        context.RegisterSourceOutput(completed, static (productionContext, result) => EmitCompleted(productionContext, result));

        // レジストリ初期化子（全アクセサを横断して集約。symbol 由来で SQL 不要）。
        // .Collect() がストリームを 1 つの ImmutableArray に畳み込み、全アクセサを一度に渡す。レジストリ
        // （DataAccessorRegistryInitializer.g.cs）は全アクセサの DI 登録を集約する単一ファイルでクラス毎に分割できないため、
        // RegisterSourceOutput は集合全体で 1 回だけ走る。集合が変わった時（アクセサの追加/削除、登録関連データの変化）に
        // その小さな 1 ファイルだけを再出力する。
        // Registry initializer (aggregated across all accessors; symbol-derived, no SQL needed). `.Collect()`
        // collapses the stream into a single ImmutableArray holding every accessor at once. The registry
        // (DataAccessorRegistryInitializer.g.cs) is one file aggregating every accessor's DI registration and
        // cannot be split per class, so RegisterSourceOutput runs once over the whole set; it re-emits that one
        // small file whenever the set changes (an accessor added/removed, or any registration-relevant data).
        context.RegisterSourceOutput(completed.Collect(), static (productionContext, all) => EmitRegistry(productionContext, all));

        // /*!using*/ と /*!helper*/ は Compilation に対して検証しない。無効な名前空間・ヘルパー型は生成された
        // using 行の C# エラーとして現れるため、専用診断は出さない（パイプラインを Compilation 非依存・完全キャッシュに保つ）。
        // /*!using*/ and /*!helper*/ are not validated against the Compilation. An invalid namespace or helper
        // type surfaces as a C# error on the generated `using` line, so no dedicated diagnostic is emitted
        // (this keeps the pipeline Compilation-free and fully cached).
    }

    // 1 アクセサ分の出力：診断を報告し、Model があれば AccessorSourceBuilder でソースを生成して {ns}_{Class}.g.cs を追加する。
    // Emit one accessor: report its diagnostics, then (if a model exists) generate its source via
    // AccessorSourceBuilder and add it as {ns}_{Class}.g.cs.
    private static void EmitCompleted(SourceProductionContext context, Result<AccessorModel> result)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }
        if (result.Value is not { } model)
        {
            return;
        }
        var source = AccessorSourceBuilder.Emit(model);
        var ns = String.IsNullOrEmpty(model.Namespace) ? "global" : model.Namespace.Replace('.', '_');
        var filename = $"{ns}_{model.ClassName}.g.cs";
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    }

    // 全アクセサを集約し、DI 登録に要る情報（サービス型 / 具象型 / プロバイダ要否 / [Inject] 型）を RegistryEntry に集める。
    // 1 件以上あれば、各アクセサを登録する ModuleInitializer 初期化子ファイルを出力する。
    // Aggregate all accessors, gathering the data needed for DI registration (service/concrete type, whether a
    // provider is required, [Inject] types) into RegistryEntry; if there is at least one, emit an initializer
    // file that registers every accessor from a ModuleInitializer.
    private static void EmitRegistry(SourceProductionContext context, ImmutableArray<Result<AccessorModel>> all)
    {
        var registrations = new List<RegistryEntry>();
        foreach (var result in all)
        {
            if (result.Value is not { } model)
            {
                continue;
            }
            var concreteName = String.IsNullOrEmpty(model.Namespace)
                ? $"global::{model.ClassName}"
                : $"global::{model.Namespace}.{model.ClassName}";
            registrations.Add(new RegistryEntry(
                model.ServiceTypeFullName ?? concreteName,
                concreteName,
                model.RequiresConnectionFactory,
                model.ProviderName is not null,
                model.Injects.Select(x => x.TypeFullName).ToArray()));
        }

        if (registrations.Count > 0)
        {
            var initializer = EmitRegistryInitializer(registrations);
            context.AddSource("DataAccessorRegistryInitializer.g.cs", SourceText.From(initializer, Encoding.UTF8));
        }
    }

    private sealed record RegistryEntry(
        string ServiceTypeName,
        string ConcreteTypeName,
        bool RequiresProvider,
        bool MultiProvider,
        IReadOnlyList<string> InjectTypeFqs);

    // ModuleInitializer 内で DataAccessorRegistry.Register<T>(factory) を 1 件ずつ生成する。各 factory は
    // provider.GetService 経由でプロバイダ／[Inject] 依存を解決し、アクセサを new する。
    // Build the initializer: emit one DataAccessorRegistry.Register<T>(factory) per accessor inside a
    // ModuleInitializer. Each factory resolves its provider / [Inject] dependencies via provider.GetService and news up the accessor.
    private static string EmitRegistryInitializer(List<RegistryEntry> entries)
    {
        var builder = new SourceBuilder();
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Indent().Append("#pragma warning disable").NewLine();
        builder.NewLine();
        builder.Indent().Append("internal static class DataAccessorRegistryInitializer").NewLine();
        builder.BeginScope();
        builder.Indent().Append("[global::System.Runtime.CompilerServices.ModuleInitializer]").NewLine();
        builder.Indent().Append("internal static void Initialize()").NewLine();
        builder.BeginScope();
        foreach (var entry in entries)
        {
            var args = new List<string>();
            if (entry.RequiresProvider)
            {
                var providerName = entry.MultiProvider
                    ? "global::Smart.Data.IDbProviderSelector"
                    : "global::Smart.Data.IDbProvider";
                args.Add($"({providerName})provider.GetService(typeof({providerName}))!");
            }
            foreach (var injectName in entry.InjectTypeFqs)
            {
                args.Add($"({injectName})provider.GetService(typeof({injectName}))!");
            }
            builder.Indent()
                .Append("global::Smart.Data.Accessor.DataAccessorRegistry.Register<")
                .Append(entry.ServiceTypeName)
                .Append(">(static provider => new ")
                .Append(entry.ConcreteTypeName)
                .Append("(")
                .Append(String.Join(", ", args))
                .Append("));")
                .NewLine();
        }
        builder.EndScope();
        builder.EndScope();
        return builder.ToString();
    }
}
