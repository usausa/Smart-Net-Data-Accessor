namespace Smart.Data.Accessor.Generator.Tests;

using System.Collections.Immutable;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Builders.Generator;
using Smart.Data.Accessor.Builders.MySql.Generator;
using Smart.Data.Accessor.Builders.Postgres.Generator;
using Smart.Data.Accessor.Builders.SqlServer.Generator;

// Runs the two Smart.Data.Accessor source generators (core DataAccessorGenerator +
// Builders QueryBuilderGenerator) together in-memory and exposes the reported diagnostics
// and generated sources for assertion. Mirrors __Reference/GeneratorSample's harness.
internal static class GeneratorTestHelper
{
    // The generators depend on SourceGenerateHelper at run time. It is copied next to the test
    // binaries (PackageReference), so load it eagerly to avoid a first-use resolve failure.
    private static readonly Lazy<bool> EnsureDeps = new(static () =>
    {
        var dir = Path.GetDirectoryName(typeof(GeneratorTestHelper).Assembly.Location)!;
        var helper = Path.Combine(dir, "SourceGenerateHelper.dll");
        if (File.Exists(helper))
        {
            Assembly.LoadFrom(helper);
        }
        return true;
    });

    // Result of one in-memory generator run.
    internal sealed class RunResult
    {
        public required IReadOnlyList<Diagnostic> Diagnostics { get; init; }

        public required IReadOnlyDictionary<string, string> GeneratedSources { get; init; }

        // All generated sources concatenated; convenient for substring assertions.
        public required string AllGeneratedText { get; init; }
    }

    // Run both generators against the given C# source plus optional SQL files. Each SQL file is
    // exposed as an AdditionalText under a "Sql" folder so the core generator's resolver picks it
    // up; name it "{ClassName}.{MethodName}" to match a method.
    internal static RunResult Run(string source, params (string Name, string Sql)[] sqlFiles)
    {
        _ = EnsureDeps.Value;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);

        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [syntaxTree],
            references: BuildReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var additionalTexts = sqlFiles
            .Select(static f => (AdditionalText)new InMemoryAdditionalText($"/proj/Sql/{f.Name}.sql", f.Sql))
            .ToImmutableArray();

        var driver = CSharpGeneratorDriver.Create(
            generators:
            [
                new DataAccessorGenerator().AsSourceGenerator(),
                new QueryBuilderGenerator().AsSourceGenerator(),
                new SqlServerQueryBuilderGenerator().AsSourceGenerator(),
                new MySqlQueryBuilderGenerator().AsSourceGenerator(),
                new PostgresQueryBuilderGenerator().AsSourceGenerator()
            ],
            additionalTexts: additionalTexts,
            parseOptions: parseOptions);

        var runResult = driver.RunGenerators(compilation).GetRunResult();

        var generated = new Dictionary<string, string>(StringComparer.Ordinal);
        var all = new StringBuilder();
        foreach (var result in runResult.Results)
        {
            foreach (var generatedSource in result.GeneratedSources)
            {
                var text = generatedSource.SourceText.ToString();
                generated[generatedSource.HintName] = text;
                all.Append(text).AppendLine();
            }
        }

        return new RunResult
        {
            Diagnostics = runResult.Diagnostics,
            GeneratedSources = generated,
            AllGeneratedText = all.ToString()
        };
    }

    // Convenience: only the SDA####/SDB#### diagnostics the generators report.
    internal static IReadOnlyList<Diagnostic> GetDiagnostics(string source, params (string Name, string Sql)[] sqlFiles) =>
        Run(source, sqlFiles)
            .Diagnostics
            .Where(static d =>
                d.Id.StartsWith("SDA", StringComparison.Ordinal) ||
                d.Id.StartsWith("SDB", StringComparison.Ordinal))
            .ToList();

    // For incremental-cache regression tests: a driver with step tracking enabled
    // plus the compilation. Only the core DataAccessorGenerator is wired (the unit under test).
    internal static (GeneratorDriver Driver, Compilation Compilation) CreateTrackingDriver(
        string source, params (string Name, string Sql)[] sqlFiles)
    {
        _ = EnsureDeps.Value;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references: BuildReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var additionalTexts = sqlFiles
            .Select(static f => (AdditionalText)new InMemoryAdditionalText($"/proj/Sql/{f.Name}.sql", f.Sql))
            .ToImmutableArray();

        var driver = CSharpGeneratorDriver.Create(
            generators: [new DataAccessorGenerator().AsSourceGenerator()],
            additionalTexts: additionalTexts,
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

        return (driver, compilation);
    }

    // For Builder incremental-cache regression tests: a driver with step tracking
    // enabled wiring the (ANSI) QueryBuilderGenerator. The Builder generators have no .sql dependency,
    // so no AdditionalText is needed.
    internal static (GeneratorDriver Driver, Compilation Compilation) CreateBuilderTrackingDriver(string source)
    {
        _ = EnsureDeps.Value;

        var parseOptions = new CSharpParseOptions(LanguageVersion.Preview);
        var compilation = CSharpCompilation.Create(
            assemblyName: "GeneratorTestAssembly",
            syntaxTrees: [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references: BuildReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        var driver = CSharpGeneratorDriver.Create(
            generators: [new QueryBuilderGenerator().AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true));

        return (driver, compilation);
    }

    // Use every assembly the test host trusts as a metadata reference. This includes the runtime
    // attribute assemblies (Smart.Data.Accessor[.Builders]) and their dependencies, which are
    // copied next to the test binaries.
    private static List<MetadataReference> BuildReferences()
    {
        var references = new List<MetadataReference>();
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
        {
            foreach (var path in trusted.Split(Path.PathSeparator))
            {
                if (!String.IsNullOrEmpty(path) &&
                    path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }
        return references;
    }

    private sealed class InMemoryAdditionalText : AdditionalText
    {
        private readonly SourceText text;

        public InMemoryAdditionalText(string path, string content)
        {
            Path = path;
            text = SourceText.From(content, Encoding.UTF8);
        }

        public override string Path { get; }

        public override SourceText GetText(CancellationToken cancellationToken = default) => text;
    }
}
