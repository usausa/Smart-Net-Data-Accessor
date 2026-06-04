namespace Smart.Data.Accessor.Benchmark;

using System.Data;
using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

using Dapper;

using Smart.Mock.Data;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet requires benchmark configuration types to be public.")]
public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddDiagnoser(
            MemoryDiagnoser.Default,
            new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(
                maxDepth: 3,
                printSource: true,
                printInstructionAddresses: true,
                exportDiff: true)));
        AddJob(Job.MediumRun);
    }
}

[Config(typeof(BenchmarkConfig))]
[SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "Mock connections are disposed in [GlobalCleanup]; BenchmarkDotNet manages benchmark instance lifecycle.")]
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet requires benchmark classes to be public.")]
public class QueryBenchmark
{
    private const int RowCount = 100;

    private MockRepeatDbConnection mockInt = default!;
    private MockRepeatDbConnection mockWide = default!;
    private MockRepeatDbConnection mockEnum = default!;
    private MockRepeatDbConnection mockTicks = default!;

    private BenchmarkAccessor accessor = default!;

    [GlobalSetup]
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "MockDataReader ownership is transferred to MockRepeatDbConnection; readers are disposed transitively when the mock connections are disposed in [GlobalCleanup].")]
    public void Setup()
    {
        mockInt = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[]
            {
                (long)x,
            })));

        mockWide = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(int), "Age"),
                new MockColumn(typeof(double), "Score"),
                new MockColumn(typeof(bool), "Active"),
                new MockColumn(typeof(int), "Status"),
                new MockColumn(typeof(string), "Description"),
                new MockColumn(typeof(int), "Category"),
                new MockColumn(typeof(string), "Tag"),
                new MockColumn(typeof(double), "Weight"),
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[]
            {
                (long)x,
                $"Name-{x}",
                x % 80,
                x * 1.5,
                (x % 2) == 0,
                x % 4,
                $"Description-{x}",
                x % 8,
                $"Tag-{x}",
                x * 0.25,
            })));

        mockEnum = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(int), "Status"),
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[]
            {
                (long)x,
                $"Name-{x}",
                x % 4,
            })));

        var baseTicks = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
        mockTicks = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(long), "Created"),
            ],
            Enumerable.Range(1, RowCount).Select(x => new object[]
            {
                (long)x,
                baseTicks + (x * TimeSpan.TicksPerSecond),
            })));

        accessor = new BenchmarkAccessor();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        mockInt.Dispose();
        mockWide.Dispose();
        mockEnum.Dispose();
        mockTicks.Dispose();
    }

    // -----------------------------------------------------------------
    // Case 1: 1-column int SELECT
    // -----------------------------------------------------------------

    [Benchmark(Description = "Case1: Generator (1-col int)")]
    public IReadOnlyList<BenchIntRow> Case1Generator() => accessor.QueryInt(mockInt);

    [Benchmark(Description = "Case1: Manual ADO.NET (1-col int)")]
    public List<BenchIntRow> Case1Manual()
    {
        var list = new List<BenchIntRow>();
        using var cmd = mockInt.CreateCommand();
        cmd.CommandText = "SELECT Id FROM BenchData ORDER BY Id";
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
        var ordId = reader.GetOrdinal("Id");
        while (reader.Read())
        {
            list.Add(new BenchIntRow { Id = reader.GetInt64(ordId) });
        }
        return list;
    }

    [Benchmark(Description = "Case1: Dapper (1-col int)")]
    public List<BenchIntRow> Case1Dapper() => mockInt.Query<BenchIntRow>("SELECT Id FROM BenchData ORDER BY Id").AsList();

    // -----------------------------------------------------------------
    // Case 2: 10-column mixed SELECT
    // -----------------------------------------------------------------

    [Benchmark(Description = "Case2: Generator (10-col mixed)")]
    public IReadOnlyList<BenchWideRow> Case2Generator() => accessor.QueryWide(mockWide);

    [Benchmark(Description = "Case2: Manual ADO.NET (10-col mixed)")]
    public List<BenchWideRow> Case2Manual()
    {
        var list = new List<BenchWideRow>();
        using var cmd = mockWide.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Age, Score, Active, Status, Description, Category, Tag, Weight FROM BenchData ORDER BY Id";
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
        var ordId = reader.GetOrdinal("Id");
        var ordName = reader.GetOrdinal("Name");
        var ordAge = reader.GetOrdinal("Age");
        var ordScore = reader.GetOrdinal("Score");
        var ordActive = reader.GetOrdinal("Active");
        var ordStatus = reader.GetOrdinal("Status");
        var ordDescription = reader.GetOrdinal("Description");
        var ordCategory = reader.GetOrdinal("Category");
        var ordTag = reader.GetOrdinal("Tag");
        var ordWeight = reader.GetOrdinal("Weight");
        while (reader.Read())
        {
            list.Add(new BenchWideRow
            {
                Id = reader.GetInt64(ordId),
                Name = reader.GetString(ordName),
                Age = reader.GetInt32(ordAge),
                Score = reader.GetDouble(ordScore),
                Active = reader.GetBoolean(ordActive),
                Status = reader.GetInt32(ordStatus),
                Description = reader.GetString(ordDescription),
                Category = reader.GetInt32(ordCategory),
                Tag = reader.GetString(ordTag),
                Weight = reader.GetDouble(ordWeight),
            });
        }
        return list;
    }

    [Benchmark(Description = "Case2: Dapper (10-col mixed)")]
    public List<BenchWideRow> Case2Dapper() => mockWide.Query<BenchWideRow>("SELECT Id, Name, Age, Score, Active, Status, Description, Category, Tag, Weight FROM BenchData ORDER BY Id").AsList();

    // -----------------------------------------------------------------
    // Case 3: SELECT containing enum column
    // -----------------------------------------------------------------

    [Benchmark(Description = "Case3: Generator (enum)")]
    public IReadOnlyList<BenchEnumRow> Case3Generator() => accessor.QueryWithEnum(mockEnum);

    [Benchmark(Description = "Case3: Manual ADO.NET (enum)")]
    public List<BenchEnumRow> Case3Manual()
    {
        var list = new List<BenchEnumRow>();
        using var cmd = mockEnum.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Status FROM BenchData ORDER BY Id";
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
        var ordId = reader.GetOrdinal("Id");
        var ordName = reader.GetOrdinal("Name");
        var ordStatus = reader.GetOrdinal("Status");
        while (reader.Read())
        {
            list.Add(new BenchEnumRow
            {
                Id = reader.GetInt64(ordId),
                Name = reader.GetString(ordName),
                Status = (BenchStatus)reader.GetInt32(ordStatus),
            });
        }
        return list;
    }

    [Benchmark(Description = "Case3: Dapper (enum)")]
    public List<BenchEnumRow> Case3Dapper() => mockEnum.Query<BenchEnumRow>("SELECT Id, Name, Status FROM BenchData ORDER BY Id").AsList();

    // -----------------------------------------------------------------
    // Case 4: [TypeHandler<>] column (DateTime stored as Int64 ticks)
    // -----------------------------------------------------------------

    [Benchmark(Description = "Case4: Generator (TypeHandler)")]
    public IReadOnlyList<BenchTicksRow> Case4Generator() => accessor.QueryTicks(mockTicks);

    [Benchmark(Description = "Case4: Manual ADO.NET (TypeHandler)")]
    public List<BenchTicksRow> Case4Manual()
    {
        var list = new List<BenchTicksRow>();
        using var cmd = mockTicks.CreateCommand();
        cmd.CommandText = "SELECT Id, Created FROM BenchData ORDER BY Id";
        using var reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
        var ordId = reader.GetOrdinal("Id");
        var ordCreated = reader.GetOrdinal("Created");
        while (reader.Read())
        {
            list.Add(new BenchTicksRow
            {
                Id = reader.GetInt64(ordId),
                Created = new DateTime(reader.GetInt64(ordCreated), DateTimeKind.Utc),
            });
        }
        return list;
    }
}
