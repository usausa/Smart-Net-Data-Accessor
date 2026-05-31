namespace Smart.Data.Accessor.Benchmark;

using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

using Microsoft.Data.Sqlite;

using Smart.Data.Accessor.Connection;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet requires benchmark configuration types to be public.")]
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.ShortRun);
    }
}

[Config(typeof(BenchmarkConfig))]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Connection is disposed in [GlobalCleanup]; BenchmarkDotNet manages benchmark instance lifecycle.")]
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet requires benchmark classes to be public.")]
public class QueryBenchmark
{
    private const int RowCount = 100;

    private const string ConnectionString = "Data Source=file:bench-mem-db?mode=memory&cache=shared";

    private SqliteConnection connection = default!;
    private BenchmarkAccessor accessor = default!;

    [GlobalSetup]
    public void Setup()
    {
        connection = new SqliteConnection(ConnectionString);
        connection.Open();

        accessor = new BenchmarkAccessor(new SharedSqliteConnectionFactory(ConnectionString));
        accessor.CreateTable();

        for (var i = 1; i <= RowCount; i++)
        {
            accessor.InsertRow(
                id: i,
                name: $"Name-{i}",
                age: i % 80,
                score: i * 1.5,
                active: (i % 2) == 0,
                status: i % 4,
                description: $"Description-{i}",
                category: i % 8,
                tag: $"Tag-{i}",
                weight: i * 0.25);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        connection.Dispose();
    }

    // -----------------------------------------------------------------
    // Case 1: 1-column int SELECT
    // -----------------------------------------------------------------

    [Benchmark(Description = "Case1: Generator (1-col int)")]
    public IReadOnlyList<BenchIntRow> Case1Generator() => accessor.QueryInt();

    [Benchmark(Description = "Case1: Manual ADO.NET (1-col int)")]
    public List<BenchIntRow> Case1Manual()
    {
        var list = new List<BenchIntRow>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id FROM BenchData ORDER BY Id";
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
        while (reader.Read())
        {
            list.Add(new BenchIntRow { Id = reader.GetInt64(0) });
        }
        return list;
    }

    // -----------------------------------------------------------------
    // Case 2: 10-column mixed SELECT
    // -----------------------------------------------------------------

    [Benchmark(Description = "Case2: Generator (10-col mixed)")]
    public IReadOnlyList<BenchWideRow> Case2Generator() => accessor.QueryWide();

    [Benchmark(Description = "Case2: Manual ADO.NET (10-col mixed)")]
    public List<BenchWideRow> Case2Manual()
    {
        var list = new List<BenchWideRow>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Age, Score, Active, Status, Description, Category, Tag, Weight FROM BenchData ORDER BY Id";
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
        while (reader.Read())
        {
            list.Add(new BenchWideRow
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Age = reader.GetInt32(2),
                Score = reader.GetDouble(3),
                Active = reader.GetInt32(4) != 0,
                Status = reader.GetInt32(5),
                Description = reader.GetString(6),
                Category = reader.GetInt32(7),
                Tag = reader.GetString(8),
                Weight = reader.GetDouble(9),
            });
        }
        return list;
    }

    // -----------------------------------------------------------------
    // Case 3: SELECT containing enum column
    // -----------------------------------------------------------------

    [Benchmark(Description = "Case3: Generator (enum)")]
    public IReadOnlyList<BenchEnumRow> Case3Generator() => accessor.QueryWithEnum();

    [Benchmark(Description = "Case3: Manual ADO.NET (enum)")]
    public List<BenchEnumRow> Case3Manual()
    {
        var list = new List<BenchEnumRow>();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Status FROM BenchData ORDER BY Id";
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
        while (reader.Read())
        {
            list.Add(new BenchEnumRow
            {
                Id = reader.GetInt64(0),
                Name = reader.GetString(1),
                Status = (BenchStatus)reader.GetInt32(2),
            });
        }
        return list;
    }
}
