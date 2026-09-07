namespace Smart.Data.Accessor.Generator;

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

[Generator]
public sealed class DataAccessorGenerator : IIncrementalGenerator
{
    private const string SqlFolderProperty = "build_property.SmartDataAccessor_SqlFolder";
    private const string SkipLocalsInitProperty = "build_property.SmartDataAccessor_SkipLocalsInit";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // === インクリメンタル生成パイプラインの配線(本体は配線のみで、実処理は 2 層に分離している)===
        //   AccessorModelBuilder … transform 段：symbol を等価な Result<AccessorModel>(Model ＋ 診断)へ変換
        //   AccessorSourceBuilder … emit 段：Model を生成 C# 文字列へ。symbol 非依存なので単体テスト可能
        // === Wiring of the incremental generation pipeline (this type only wires; the work is split in two) ===
        //   AccessorModelBuilder  — transform stage: symbol -> equatable Result<AccessorModel> (model + diagnostics)
        //   AccessorSourceBuilder — emit stage: Model -> generated C# string; symbol-free, so unit-testable

        // MSBuild プロパティ由来の生成オプション。.targets が CompilerVisibleProperty として公開したものを
        // OptionModel 1 つに束ねて流す。record の値等価性でキャッシュが効く。
        // Generation options coming from MSBuild properties. Everything the .targets exposes as a
        // CompilerVisibleProperty is bundled into a single OptionModel; the record's value equality keeps the
        // pipeline cached.
        var optionProvider = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => SelectOption(provider));

        // 追加ファイル(AdditionalText)から .sql を集め、(ファイル名, 本文) に射影し、SQL フォルダ名と結合する。
        // Collect .sql additional files, project each to (file name, text), and combine them with the SQL folder name.
        var sqlFiles = context.AdditionalTextsProvider
            .Where(static x => x.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(static (t, cancellation) => (
                FullPath: t.Path,
                Path: Path.GetFileNameWithoutExtension(t.Path),
                Text: t.GetText(cancellation)?.ToString() ?? string.Empty))
            .Combine(optionProvider)
            .Where(static pair =>
            {
                // 親ディレクトリ名が {SqlFolder} に一致する .sql だけを対象にする(無関係な .sql を除外)。
                // Keep only .sql whose parent directory name matches {SqlFolder} (excludes unrelated .sql files).
                var parentDir = Path.GetFileName(Path.GetDirectoryName(pair.Left.FullPath));
                return String.Equals(parentDir, pair.Right.SqlFolder, StringComparison.OrdinalIgnoreCase);
            })
            .Select(static (pair, _) => (pair.Left.Path, pair.Left.Text))
            .Collect();

        // [DataAccessor] クラスを FAWMN で拾い、transform 段で等価な Result<AccessorModel>(Model ＋ 診断)へ変換する。
        // ここがインクリメンタルキャッシュの境界。SQL の解決・解析は .sql を要するので後段(出力段)に置き、
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
        // RegisterSourceOutput は要素(＝クラス)毎に 1 回走り、そのクラスの {ns}_{Class}.g.cs を出力する。
        // 自分の Result<AccessorModel> が変わったアクセサだけ再生成され、変化のないものはキャッシュされ skip される(クラス単位の粒度)。
        // Emit per-accessor source + diagnostics. `completed` is a stream with one element per [DataAccessor]
        // class, so RegisterSourceOutput runs once per element (= per class), emitting that class's
        // {ns}_{Class}.g.cs. Only accessors whose own Result<AccessorModel> changed are re-emitted; unchanged
        // ones stay cached and are skipped (per-class granularity).
        context.RegisterSourceOutput(
            completed.Combine(optionProvider),
            static (productionContext, pair) => EmitCompleted(productionContext, pair.Left, pair.Right));

        // レジストリ初期化子(全アクセサを横断して集約。symbol 由来で SQL 不要)。
        // .Collect() がストリームを 1 つの ImmutableArray に畳み込み、全アクセサを一度に渡す。レジストリ
        // (DataAccessorRegistryInitializer.g.cs)は全アクセサの DI 登録を集約する単一ファイルでクラス毎に分割できないため、
        // RegisterSourceOutput は集合全体で 1 回だけ走る。集合が変わった時(アクセサの追加/削除、登録関連データの変化)に
        // その小さな 1 ファイルだけを再出力する。
        // Registry initializer (aggregated across all accessors; symbol-derived, no SQL needed). `.Collect()`
        // collapses the stream into a single ImmutableArray holding every accessor at once. The registry
        // (DataAccessorRegistryInitializer.g.cs) is one file aggregating every accessor's DI registration and
        // cannot be split per class, so RegisterSourceOutput runs once over the whole set; it re-emits that one
        // small file whenever the set changes (an accessor added/removed, or any registration-relevant data).
        var registryEntries = completed
            .SelectMany(static (result, _) => CreateRegistryEntries(result))
            .Collect();
        context.RegisterSourceOutput(registryEntries, static (productionContext, all) => EmitRegistry(productionContext, all));

        // [DataAccessorRegistration] partial メソッド(IServiceCollection 拡張)。宣言を transform 段で検証して Model 化し、
        // レジストリと同じアクセサ集合と結合して実装部を出力する。宣言の引数型が IServiceCollection であること自体が
        // M.E.DI 抽象への参照の証明なので、Compilation は参照しない。
        // [DataAccessorRegistration] partial methods (IServiceCollection extensions). Each declaration is validated and
        // modeled in the transform stage, then combined with the same accessor set as the registry to emit the
        // implementation part. The IServiceCollection parameter type of the declaration proves the M.E.DI abstractions
        // reference, so the Compilation is never consulted.
        var registrationMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                RegistrationModelBuilder.DataAccessorRegistrationAttributeName,
                static (x, _) => x is MethodDeclarationSyntax,
                static (context, _) => RegistrationModelBuilder.BuildMethodResult(context))
            .WithTrackingName("RegistrationMethodResult")
            .Collect();
        context.RegisterSourceOutput(
            registrationMethods.Combine(registryEntries),
            static (productionContext, pair) => EmitRegistrations(productionContext, pair.Left, pair.Right));

        // /*!using*/ と /*!helper*/ は Compilation に対して検証しない。無効な名前空間・ヘルパー型は生成された
        // using 行の C# エラーとして現れるため、専用診断は出さない(パイプラインを Compilation 非依存・完全キャッシュに保つ)。
        // /*!using*/ and /*!helper*/ are not validated against the Compilation. An invalid namespace or helper
        // type surfaces as a C# error on the generated `using` line, so no dedicated diagnostic is emitted
        // (this keeps the pipeline Compilation-free and fully cached).
    }

    // MSBuild プロパティを OptionModel へ束ねる。
    // SqlFolder はプロジェクト毎に <SmartDataAccessor_SqlFolder> で変更できる(既定 "Sql")。
    // SkipLocalsInit は <SmartDataAccessor_SkipLocalsInit> で切り替える。.targets が既定値と、属性が要求する
    // AllowUnsafeBlocks の両方を面倒みるので、ここは値を読むだけでよい。値が取れないときに true を既定にすると
    // .targets を取り込んでいないプロジェクトで CS0227 になりうるため、取得できたときだけ有効とする。
    // Bundle the MSBuild properties into an OptionModel.
    // SqlFolder is configurable per project via <SmartDataAccessor_SqlFolder> (default "Sql").
    // SkipLocalsInit is controlled by <SmartDataAccessor_SkipLocalsInit>; the .targets supplies both its default
    // and the AllowUnsafeBlocks the attribute needs, so this only reads the value. Defaulting a missing value to
    // true would risk CS0227 in a project that never imported the .targets, so it counts as enabled only when
    // the value is actually present.
    private static OptionModel SelectOption(AnalyzerConfigOptionsProvider provider)
    {
        var sqlFolder = provider.GlobalOptions.TryGetValue(SqlFolderProperty, out var folder) && !String.IsNullOrWhiteSpace(folder)
            ? folder
            : OptionModel.Default.SqlFolder;
        var skipLocalsInit = provider.GlobalOptions.TryGetValue(SkipLocalsInitProperty, out var value) &&
            Boolean.TryParse(value, out var result) &&
            result;
        return new OptionModel(sqlFolder, skipLocalsInit);
    }

    // 1 アクセサ分の出力：診断を報告し、Model があれば AccessorSourceBuilder でソースを生成して {ns}_{Class}.g.cs を追加する。
    // Emit one accessor: report its diagnostics, then (if a model exists) generate its source via
    // AccessorSourceBuilder and add it as {ns}_{Class}.g.cs.
    private static void EmitCompleted(SourceProductionContext context, Result<AccessorModel> result, OptionModel option)
    {
        foreach (var diagnostic in result.Diagnostics)
        {
            context.ReportDiagnostic(diagnostic.ToDiagnostic());
        }
        if (result.Value is not { } model)
        {
            return;
        }
        var source = AccessorSourceBuilder.Emit(model, option);
        // This repository names the global namespace "global" rather than omitting the segment,
        // which is what HintNameBuilder would do on its own. Keep the existing hint names.
        var ns = String.IsNullOrEmpty(model.Namespace) ? "global" : model.Namespace;
        context.AddSource(HintNameBuilder.Build(ns, model.ClassName), SourceText.From(source, Encoding.UTF8));
    }

    // 全アクセサを集約し、DI 登録に要る情報(サービス型 / 具象型 / プロバイダ要否 / [Inject] 型)を RegistryEntry に集める。
    // 1 件以上あれば、各アクセサを登録する ModuleInitializer 初期化子ファイルを出力する。
    // Aggregate all accessors, gathering the data needed for DI registration (service/concrete type, whether a
    // provider is required, [Inject] types) into RegistryEntry; if there is at least one, emit an initializer
    // file that registers every accessor from a ModuleInitializer.
    private static ImmutableArray<RegistryEntry> CreateRegistryEntries(Result<AccessorModel> result)
    {
        if (result.Value is not { } model)
        {
            return [];
        }

        var concreteName = String.IsNullOrEmpty(model.Namespace)
            ? $"global::{model.ClassName}"
            : $"global::{model.Namespace}.{model.ClassName}";
        return
        [
            new RegistryEntry(
                model.Namespace,
                model.ServiceTypeFullName ?? concreteName,
                concreteName,
                model.RequiresConnectionFactory,
                model.ProviderName is not null,
                new EquatableArray<string>(model.Injects.Select(static x => x.TypeFullName)))
        ];
    }

    private static void EmitRegistry(SourceProductionContext context, ImmutableArray<RegistryEntry> registrations)
    {
        if (registrations.Length > 0)
        {
            var initializer = EmitRegistryInitializer(registrations);
            context.AddSource("DataAccessorRegistryInitializer.g.cs", SourceText.From(initializer, Encoding.UTF8));
        }
    }

    private sealed record RegistryEntry(
        string Namespace,
        string ServiceTypeName,
        string ConcreteTypeName,
        bool RequiresProvider,
        bool MultiProvider,
        EquatableArray<string> InjectTypeFqs);

    // ModuleInitializer 内で DataAccessorRegistry.Register<T>(factory) を 1 件ずつ生成する。各 factory は
    // provider.GetService 経由でプロバイダ／[Inject] 依存を解決し、アクセサを new する。
    // Build the initializer: emit one DataAccessorRegistry.Register<T>(factory) per accessor inside a
    // ModuleInitializer. Each factory resolves its provider / [Inject] dependencies via provider.GetService and news up the accessor.
    private static string EmitRegistryInitializer(ImmutableArray<RegistryEntry> entries)
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
            var args = EnumerateDependencyTypes(entry)
                .Select(static x => $"({x})provider.GetService(typeof({x}))!")
                .ToList();
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

    // 生成コンストラクタの引数順(プロバイダ → [Inject])で依存型を列挙する。レジストリ初期化子と登録メソッドで共用。
    // Enumerates the dependency types in the generated constructor's parameter order (provider, then [Inject]).
    // Shared by the registry initializer and the registration methods.
    private static IEnumerable<string> EnumerateDependencyTypes(RegistryEntry entry)
    {
        if (entry.RequiresProvider)
        {
            yield return entry.MultiProvider
                ? "global::Smart.Data.IDbProviderSelector"
                : "global::Smart.Data.IDbProvider";
        }
        foreach (var injectName in entry.InjectTypeFqs)
        {
            yield return injectName;
        }
    }

    // [DataAccessorRegistration] メソッドの実装部を出力する。診断を報告し、有効な宣言を(名前空間, クラス)単位にまとめて
    // {ns}_{Class}.Registration.g.cs を 1 ファイルずつ出力する。
    // Emits the implementation parts of the [DataAccessorRegistration] methods: reports the diagnostics, groups the
    // valid declarations by (namespace, class) and emits one {ns}_{Class}.Registration.g.cs per class.
    private static void EmitRegistrations(
        SourceProductionContext context,
        ImmutableArray<Result<RegistrationMethodModel>> methods,
        ImmutableArray<RegistryEntry> entries)
    {
        var groups = new List<(string Namespace, string ClassName, List<RegistrationMethodModel> Methods)>();
        foreach (var result in methods)
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                context.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
            if (result.Value is not { } model)
            {
                continue;
            }

            var index = groups.FindIndex(x => (x.Namespace == model.Namespace) && (x.ClassName == model.ClassName));
            if (index < 0)
            {
                groups.Add((model.Namespace, model.ClassName, [model]));
            }
            else
            {
                groups[index].Methods.Add(model);
            }
        }

        if (groups.Count == 0)
        {
            return;
        }

        // 出力を安定させるため具象型名順に並べる。
        // Ordered by concrete type name so the output is stable.
        var sorted = entries
            .OrderBy(static x => x.ConcreteTypeName, StringComparer.Ordinal)
            .ToList();
        foreach (var (ns, className, classMethods) in groups)
        {
            var source = EmitRegistrationClass(context, ns, className, classMethods, sorted);
            var hintNamespace = String.IsNullOrEmpty(ns) ? "global" : ns;
            context.AddSource(HintNameBuilder.BuildWithExtension(hintNamespace, ".Registration.g.cs", className), SourceText.From(source, Encoding.UTF8));
        }
    }

    private static string EmitRegistrationClass(
        SourceProductionContext context,
        string ns,
        string className,
        List<RegistrationMethodModel> methods,
        List<RegistryEntry> entries)
    {
        var builder = new SourceBuilder();
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Indent().Append("#pragma warning disable").NewLine();
        builder.NewLine();

        if (!String.IsNullOrEmpty(ns))
        {
            builder.Namespace(ns);
            builder.NewLine();
        }

        builder.Indent().Append("partial class ").Append(className).NewLine();
        builder.BeginScope();
        for (var i = 0; i < methods.Count; i++)
        {
            if (i > 0)
            {
                builder.NewLine();
            }
            EmitRegistrationMethod(context, builder, methods[i], entries);
        }
        builder.EndScope();
        return builder.ToString();
    }

    // 1 メソッド分：対象アクセサごとに AddSingleton<サービス型>(services, static provider => new 具象型(依存...)) を出力する。
    // 依存は provider.GetRequiredService<T>() で解決する。対象が 0 件なら SDA0602 を報告し、空の本体を出力する。
    // One method: emits AddSingleton<Service>(services, static provider => new Concrete(dependencies...)) per target
    // accessor, resolving the dependencies through provider.GetRequiredService<T>(). With no target it reports
    // SDA0602 and emits an empty body.
    private static void EmitRegistrationMethod(
        SourceProductionContext context,
        SourceBuilder builder,
        RegistrationMethodModel method,
        IEnumerable<RegistryEntry> entries)
    {
        var targets = entries.Where(x => IsRegistrationTarget(method, x)).ToList();
        if (targets.Count == 0)
        {
            context.ReportDiagnostic(new DiagnosticInfo(Diagnostics.RegistrationNoTarget, method.Location, method.MethodName).ToDiagnostic());
        }

        builder.Indent()
            .Append(method.Accessibility.ToText())
            .Append(" static partial global::Microsoft.Extensions.DependencyInjection.IServiceCollection ")
            .Append(method.MethodName)
            .Append("(this global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)")
            .NewLine();
        builder.BeginScope();
        foreach (var entry in targets)
        {
            var args = EnumerateDependencyTypes(entry)
                .Select(static x => $"global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<{x}>(provider)")
                .ToList();
            builder.Indent()
                .Append("global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<")
                .Append(entry.ServiceTypeName)
                .Append(">(services, static provider => new ")
                .Append(entry.ConcreteTypeName)
                .Append("(")
                .Append(String.Join(", ", args))
                .Append("));")
                .NewLine();
        }
        builder.Indent().Append("return services;").NewLine();
        builder.EndScope();
    }

    // Namespace 未指定なら全件。指定時はその名前空間と、その配下(前方一致 + '.')を対象にする。
    // No Namespace selects everything; a filter matches the namespace itself and everything below it (prefix + '.').
    private static bool IsRegistrationTarget(RegistrationMethodModel method, RegistryEntry entry)
    {
        if (method.RegisterAll)
        {
            return true;
        }

        foreach (var filter in method.NamespaceFilters)
        {
            if ((entry.Namespace == filter) || entry.Namespace.StartsWith(filter + ".", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
