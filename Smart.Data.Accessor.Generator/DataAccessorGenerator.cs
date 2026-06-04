namespace Smart.Data.Accessor.Generator;

using System.Collections.Immutable;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// spec §7.11.4: the IIncrementalGenerator wiring only. The two builder layers it drives are split out
// for clarity and unit-testability (mirrors AmazonLambdaExtension's LambdaGenerator / LambdaModelBuilder
// / LambdaSourceBuilder split):
//   - AccessorModelBuilder : transform stage (symbol → equatable Result<AccessorModel>, diagnostics)
//   - AccessorSourceBuilder : emit stage (Model → generated C# string; symbol-free ⇒ unit-testable)
[Generator]
public sealed class DataAccessorGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // spec §3.2 / §3.2.1: SQL folder name is configurable per-project via
        // <SmartDataAccessor_SqlFolder>. The .targets exposes it via
        // CompilerVisibleProperty so the Generator can read it from
        // AnalyzerConfigOptions. Default: "Sql".
        var sqlFolder = context.AnalyzerConfigOptionsProvider.Select(static (p, _) =>
            p.GlobalOptions.TryGetValue("build_property.SmartDataAccessor_SqlFolder", out var v) &&
                !String.IsNullOrWhiteSpace(v)
                ? v
                : "Sql");

        var sqlFiles = context.AdditionalTextsProvider
            .Where(static t => t.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(static (t, ct) => (
                FullPath: t.Path,
                Path: Path.GetFileNameWithoutExtension(t.Path),
                Text: t.GetText(ct)?.ToString() ?? string.Empty))
            .Combine(sqlFolder)
            .Where(static pair =>
            {
                // spec §3.2.1: restrict to files whose parent directory name matches {SqlFolder}.
                var parentDir = Path.GetFileName(Path.GetDirectoryName(pair.Left.FullPath));
                return String.Equals(parentDir, pair.Right, StringComparison.OrdinalIgnoreCase);
            })
            .Select(static (pair, _) => (pair.Left.Path, pair.Left.Text))
            .Collect();

        // P3 (spec §7.11): symbol analysis runs in the FAWMN transform and returns an equatable
        // Result<AccessorModel> (model + diagnostics) — the incremental cache boundary. SQL resolution/parse
        // runs in the output stage (it needs the .sql files), and is Compilation-free so it caches.
        var classResults = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AccessorModelBuilder.DataAccessorAttributeName,
                static (s, _) => s is ClassDeclarationSyntax,
                static (ctx, _) => AccessorModelBuilder.BuildClassResult(ctx))
            .WithTrackingName("AccessorClassResult");

        var completed = classResults
            .Combine(sqlFiles)
            .Select(static (pair, ct) => AccessorModelBuilder.CompleteModel(pair.Left, pair.Right, ct))
            .WithTrackingName("AccessorCompleted");

        // Per-accessor source + its diagnostics.
        // `completed` is an IncrementalValuesProvider<Result<AccessorModel>> — a STREAM with one element per
        // [DataAccessor] class. RegisterSourceOutput therefore runs EmitCompleted ONCE PER ELEMENT
        // (= per accessor class), emitting that class's `{ns}_{Class}.g.cs`. Re-runs only for the
        // accessor(s) whose own Result<AccessorModel> changed; unchanged accessors stay cached and are
        // skipped (per-class granularity).
        context.RegisterSourceOutput(completed, static (spc, c) => EmitCompleted(spc, c));

        // Registry initializer (aggregated across all accessors; symbol-derived, no SQL needed).
        // `.Collect()` collapses the stream into a single IncrementalValueProvider<ImmutableArray<...>>
        // holding ALL accessors at once. RegisterSourceOutput therefore runs EmitRegistry ONCE with the
        // whole set — because the registry (`DataAccessorRegistryInitializer.g.cs`) is a single file that
        // aggregates the DI registration of every accessor and cannot be split per-class. Re-runs
        // whenever the collected set changes (an accessor added/removed, or any accessor's
        // registration-relevant data changed), and re-emits just that one small file.
        context.RegisterSourceOutput(completed.Collect(), static (spc, all) => EmitRegistry(spc, all));

        // NOTE: /*!using*/ / /*!helper*/ are NOT validated against the Compilation. An invalid namespace
        // or helper type surfaces as a C# error on the generated `using` line, so no dedicated diagnostic
        // is emitted (案C: SDA0186/0187 retired — same policy as SDA0113-0120; keeps the pipeline
        // Compilation-free and fully cached).
    }

    private static void EmitCompleted(SourceProductionContext context, Result<AccessorModel> c)
    {
        foreach (var d in c.Diagnostics)
        {
            context.ReportDiagnostic(d.ToDiagnostic());
        }
        if (c.Value is not { } model)
        {
            return;
        }
        var source = AccessorSourceBuilder.Emit(model);
        var ns = String.IsNullOrEmpty(model.Namespace) ? "global" : model.Namespace.Replace('.', '_');
        var filename = $"{ns}_{model.ClassName}.g.cs";
        context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
    }

    private static void EmitRegistry(SourceProductionContext context, ImmutableArray<Result<AccessorModel>> all)
    {
        var registrations = new List<RegistryEntry>();
        foreach (var c in all)
        {
            if (c.Value is not { } model)
            {
                continue;
            }
            var concreteFq = String.IsNullOrEmpty(model.Namespace)
                ? $"global::{model.ClassName}"
                : $"global::{model.Namespace}.{model.ClassName}";
            registrations.Add(new RegistryEntry(
                model.ServiceTypeFullName ?? concreteFq,
                concreteFq,
                model.RequiresConnectionFactory,
                model.ProviderName is not null,
                model.Injects.Select(i => i.TypeFullName).ToArray()));
        }

        if (registrations.Count > 0)
        {
            var initializer = EmitRegistryInitializer(registrations);
            context.AddSource("DataAccessorRegistryInitializer.g.cs", SourceText.From(initializer, Encoding.UTF8));
        }
    }

    private sealed record RegistryEntry(
        string ServiceTypeFq,
        string ConcreteTypeFq,
        bool RequiresProvider,
        bool MultiProvider,
        IReadOnlyList<string> InjectTypeFqs);

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
                var providerFq = entry.MultiProvider
                    ? "global::Smart.Data.IDbProviderSelector"
                    : "global::Smart.Data.IDbProvider";
                args.Add($"({providerFq})sp.GetService(typeof({providerFq}))!");
            }
            foreach (var injectFq in entry.InjectTypeFqs)
            {
                args.Add($"({injectFq})sp.GetService(typeof({injectFq}))!");
            }
            builder.Indent()
                .Append("global::Smart.Data.Accessor.DataAccessorRegistry.Register<")
                .Append(entry.ServiceTypeFq)
                .Append(">(static sp => new ")
                .Append(entry.ConcreteTypeFq)
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
