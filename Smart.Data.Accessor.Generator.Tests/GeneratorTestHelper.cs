namespace Smart.Data.Accessor.Generator.Tests;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.MySql.Generator;
using Smart.Data.Accessor.Builders.Postgres.Generator;
using Smart.Data.Accessor.Builders.SqlServer.Generator;
using Smart.Data.Accessor.Generator.Builders;

using SourceGenerateHelper.Testing;

// Runs the Smart.Data.Accessor source generators (core DataAccessorGenerator + the ANSI and
// per-provider QueryBuilderGenerators) together in-memory over the shared test harness, and
// exposes the reported diagnostics and generated sources for assertion.
internal static class GeneratorTestHelper
{
    // Each SQL file is exposed as an AdditionalText under a "Sql" folder so the core generator's
    // resolver picks it up; name it "{ClassName}.{MethodName}" to match a method.
    private static GeneratorTestRunner CreateRunner(IEnumerable<(string Name, string Sql)> sqlFiles)
    {
        var runner = new GeneratorTestRunner(
            new DataAccessorGenerator(),
            new QueryBuilderGenerator(),
            new SqlServerQueryBuilderGenerator(),
            new MySqlQueryBuilderGenerator(),
            new PostgresQueryBuilderGenerator())
            .WithDiagnosticPrefix("SDA", "SDB");

        return AddSqlFiles(runner, sqlFiles);
    }

    private static GeneratorTestRunner AddSqlFiles(
        GeneratorTestRunner runner, IEnumerable<(string Name, string Sql)> sqlFiles)
    {
        foreach (var (name, sql) in sqlFiles)
        {
            runner.WithAdditionalText($"/proj/Sql/{name}.sql", sql);
        }

        return runner;
    }

    // 生成コードを加えた Compilation の実コンパイル検証を行う：生成コードに CS エラーがあれば即例外で
    // fail させる(過去に「生成断念→CS8795 多発」を文字列 assert が素通しした既知のハーネス限界の解消)。
    // ジェネレータが Error 診断を出したケースは生成が意図的に不完全なので検証をスキップする。
    // Also verifies that the generated code actually COMPILES (adding it to the compilation and
    // failing on any CS error) — closing the known harness gap where "generation aborted → a flood
    // of CS8795" slipped past string assertions. When the generators reported an Error diagnostic,
    // generation is intentionally incomplete and the check is skipped.
    internal static GeneratorTestResult Run(string source, params (string Name, string Sql)[] sqlFiles) =>
        CreateRunner(sqlFiles).VerifyCompiles().Run(source);

    // Only the SDA####/SDB#### diagnostics the generators report.
    // 診断シナリオは生成が意図的に壊れる(partial 実装なし等)ため実コンパイル検証はしない。
    // Diagnostic scenarios intentionally leave generation incomplete (missing partial implementations
    // etc.), so the compile verification is not applied here.
    internal static IReadOnlyList<Diagnostic> GetDiagnostics(string source, params (string Name, string Sql)[] sqlFiles) =>
        CreateRunner(sqlFiles).GetDiagnostics(source);

    // For incremental-cache regression tests: a driver with step tracking enabled plus the
    // compilation. Only the core DataAccessorGenerator is wired (the unit under test).
    internal static (GeneratorDriver Driver, Compilation Compilation) CreateTrackingDriver(
        string source, params (string Name, string Sql)[] sqlFiles) =>
        AddSqlFiles(new GeneratorTestRunner(new DataAccessorGenerator()).WithTracking(), sqlFiles)
            .CreateDriver(source);

    // For Builder incremental-cache regression tests: a driver with step tracking enabled wiring
    // the (ANSI) QueryBuilderGenerator. The Builder generators have no .sql dependency, so no
    // AdditionalText is needed.
    internal static (GeneratorDriver Driver, Compilation Compilation) CreateBuilderTrackingDriver(string source) =>
        new GeneratorTestRunner(new QueryBuilderGenerator()).WithTracking().CreateDriver(source);
}
