namespace Smart.Data.Accessor.Benchmark;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;

using Dapper;

using Smart.Mock.Data;

// マッピング系ベンチ共通の実行設定(MediumRun・Memory 診断・P90 列)。
// Shared run configuration for the mapping benchmarks (MediumRun, memory diagnoser, P90 column).
public class MappingConfig : ManualConfig
{
    public MappingConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(StatisticColumn.Mean, StatisticColumn.Error, StatisticColumn.StdDev, StatisticColumn.P90);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.MediumRun);
    }
}

// 現行の生成コードを、直書き ADO.NET(Manual)と Dapper に対して比較する回帰基準ベンチ。
// 各シナリオ(列数・class/record・部分列)で Manual を baseline とし、BenchmarkDotNet の Ratio 列で性能比を出す。
// Generated 系は本物の生成アクセサ(BenchmarkAccessor)を呼ぶ。Manual は各エンティティを直接 new する下限実装。
// 実行系の変更後は本ベンチで「対直書き Ratio が悪化しない・Alloc Ratio 1.00 維持」を確認する。
//
// Run: dotnet run -c Release --project Smart.Data.Accessor.Benchmark -- --filter *DapperComparison*
#pragma warning disable CA1001
[Config(typeof(MappingConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class ComparisonBenchmark
{
    private const int RowCount = 100;
    private const string IntSql = "SELECT Id FROM BenchData ORDER BY Id";
    private const string WideSql = "SELECT Id, Name, Age, Score, Active, Status, Description, Category, Tag, Weight FROM BenchData ORDER BY Id";
    private const string EnumSql = "SELECT Id, Name, Status FROM BenchData ORDER BY Id";
    private const string SubsetSql = "SELECT Id, Name FROM BenchData ORDER BY Id";

    private MockRepeatDbConnection mockInt = default!;
    private MockRepeatDbConnection mockWide = default!;
    private MockRepeatDbConnection mockEnum = default!;
    private MockRepeatDbConnection mockSubset = default!;
    private BenchmarkAccessor accessor = default!;

    [GlobalSetup]
    public void Setup()
    {
#pragma warning disable CA2000
        mockInt = new MockRepeatDbConnection(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Enumerable.Range(1, RowCount).Select(static x => new object[] { (long)x })));

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
                new MockColumn(typeof(double), "Weight")
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[]
            {
                (long)x, $"Name-{x}", x % 80, x * 1.5, (x % 2) == 0, x % 4, $"Description-{x}", x % 8, $"Tag-{x}", x * 0.25
            })));

        mockEnum = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(int), "Status")
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[] { (long)x, $"Name-{x}", x % 4 })));

        mockSubset = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name")
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[] { (long)x, $"Name-{x}" })));
#pragma warning restore CA2000

        accessor = new BenchmarkAccessor();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        mockInt.Dispose();
        mockWide.Dispose();
        mockEnum.Dispose();
        mockSubset.Dispose();
    }

    // ----- Narrow: 1 column (long) -----

    [Benchmark(Baseline = true, Description = "Manual (直書き)")]
    [BenchmarkCategory("Narrow 1col")]
    public List<BenchIntRow> NarrowManual() => ManualMappers.QueryInt(mockInt);

    [Benchmark(Description = "Generated (現行)")]
    [BenchmarkCategory("Narrow 1col")]
    public IReadOnlyList<BenchIntRow> NarrowGenerated() => accessor.QueryInt(mockInt);

    [Benchmark(Description = "Dapper")]
    [BenchmarkCategory("Narrow 1col")]
    public List<BenchIntRow> NarrowDapper() => mockInt.Query<BenchIntRow>(IntSql).AsList();

    // ----- Wide class: 10 columns -----

    [Benchmark(Baseline = true, Description = "Manual (直書き)")]
    [BenchmarkCategory("Wide class 10col")]
    public List<BenchWideRow> WideClassManual() => ManualMappers.QueryWide(mockWide);

    [Benchmark(Description = "Generated (現行)")]
    [BenchmarkCategory("Wide class 10col")]
    public IReadOnlyList<BenchWideRow> WideClassGenerated() => accessor.QueryWide(mockWide);

    [Benchmark(Description = "Dapper")]
    [BenchmarkCategory("Wide class 10col")]
    public List<BenchWideRow> WideClassDapper() => mockWide.Query<BenchWideRow>(WideSql).AsList();

    // ----- Wide record: 10 columns -----

    [Benchmark(Baseline = true, Description = "Manual (直書き)")]
    [BenchmarkCategory("Wide record 10col")]
    public List<BenchWideRecord> WideRecordManual() => ManualMappers.QueryWideRecord(mockWide);

    [Benchmark(Description = "Generated (現行)")]
    [BenchmarkCategory("Wide record 10col")]
    public IReadOnlyList<BenchWideRecord> WideRecordGenerated() => accessor.QueryWideRecord(mockWide);

    [Benchmark(Description = "Dapper")]
    [BenchmarkCategory("Wide record 10col")]
    public List<BenchWideRecord> WideRecordDapper() => mockWide.Query<BenchWideRecord>(WideSql).AsList();

    // ----- Enum: 3 columns (incl. enum) -----

    [Benchmark(Baseline = true, Description = "Manual (直書き)")]
    [BenchmarkCategory("Enum 3col")]
    public List<BenchEnumRow> EnumManual() => ManualMappers.QueryEnum(mockEnum);

    [Benchmark(Description = "Generated (現行)")]
    [BenchmarkCategory("Enum 3col")]
    public IReadOnlyList<BenchEnumRow> EnumGenerated() => accessor.QueryWithEnum(mockEnum);

    [Benchmark(Description = "Dapper")]
    [BenchmarkCategory("Enum 3col")]
    public List<BenchEnumRow> EnumDapper() => mockEnum.Query<BenchEnumRow>(EnumSql).AsList();

    // ----- Subset: entity has 10 props, SELECT returns 2 columns -----

    [Benchmark(Baseline = true, Description = "Manual (直書き)")]
    [BenchmarkCategory("Subset 10prop/2col")]
    public List<BenchWideRow> SubsetManual() => ManualMappers.QuerySubset(mockSubset);

    [Benchmark(Description = "Generated (現行)")]
    [BenchmarkCategory("Subset 10prop/2col")]
    public IReadOnlyList<BenchWideRow> SubsetGenerated() => accessor.QueryWide(mockSubset);

    [Benchmark(Description = "Dapper")]
    [BenchmarkCategory("Subset 10prop/2col")]
    public List<BenchWideRow> SubsetDapper() => mockSubset.Query<BenchWideRow>(SubsetSql).AsList();

    // Manual (直書き) baseline の実装は ManualMappers にある。
}
#pragma warning restore CA1001
