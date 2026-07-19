namespace Smart.Data.Accessor.Benchmark;

using System.Globalization;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using Smart.Mock.Data;

// 序数解決の戦略（FrozenDictionary / 直比較連鎖 / サンプリングハッシュ switch）を実運用の呼び出し形で比較する。
// ComparisonBenchmark と違い Dapper と無関係カテゴリを持たず、判定に要るものだけを測る。
//
// 測定設計上の要点：
//  * 1 呼び出しあたり Iterations 回のクエリをメソッド内で回し OperationsPerInvoke で割る。BenchmarkDotNet の
//    呼び出しごとのオーバーヘッドとパイロット段の揺れを薄め、MediumRun のまま総時間を短くするため。
//  * カテゴリ毎に Manual を baseline に立てる（Ratio / Alloc Ratio はカテゴリ内比較でのみ意味を持つ。
//    単一 baseline だと無関係な行同士の比が表に出て誤読を招く）。Control カテゴリは自身が baseline の
//    単独行で、Ratio は常に 1.00＝表示のためだけの行。
//  * Control(Narrow1col) は「どの閾値設定でも生成コードが同一」になる内部対照。実行間ドリフトの検出に使う
//    （閾値も戦略も 1 グループには影響しないため、ここが動いたらその実行は比較に使えない）。
//  * 戦略はエンティティの列数と閾値で決まるので、1 回の実行では 1 戦略しか測れない。閾値を変えて再ビルドし、
//    対照で実行間の比較可能性を確認したうえで突き合わせる。
//
// Compares the ordinal-resolution strategies in the shape production actually uses. Unlike ComparisonBenchmark it
// carries no Dapper or unrelated categories - only what the decision needs.
//  * Each invocation runs Iterations queries internally and divides by OperationsPerInvoke, which dilutes
//    BenchmarkDotNet's per-invocation overhead and pilot-stage jitter so MediumRun still finishes quickly.
//  * Each category has its own Manual baseline (Ratio / Alloc Ratio are only meaningful within a category; a single
//    class-wide baseline would surface cross-category ratios that invite misreading). The Control category is a
//    self-baselined single row whose Ratio is always 1.00 - it exists for display only.
//  * Control(Narrow1col) is the internal control: its generated code is identical under every threshold setting
//    (neither the threshold nor the strategy applies to a single group), so if it moves, that run cannot be compared.
//  * A single run can only measure one strategy, since the strategy follows from column count vs threshold. Rebuild
//    with a different threshold and reconcile the runs after checking the control.
#pragma warning disable CA1001
[Config(typeof(MappingConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class OrdinalStrategyBenchmark
{
    private const int RowCount = 100;

    private const int Iterations = 1_000;

    private MockRepeatDbConnection mockInt = default!;
    private MockRepeatDbConnection mockWide = default!;
    private MockRepeatDbConnection mockExtraWide = default!;
    private MockRepeatDbConnection mockExtraWideUnmapped = default!;
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

        mockExtraWide = new MockRepeatDbConnection(new MockDataReader(
            ExtraWideData.Columns(),
            Enumerable.Range(1, RowCount).Select(static x => ExtraWideData.Values(x))));

        mockExtraWideUnmapped = new MockRepeatDbConnection(new MockDataReader(
            [.. ExtraWideData.Columns().SelectMany(static (c, i) => new[]
            {
                new MockColumn(typeof(string), "unmapped_" + i.ToString(CultureInfo.InvariantCulture)),
                c
            })],
            Enumerable.Range(1, RowCount).Select(static x =>
                ExtraWideData.Values(x).SelectMany(static v => new[] { "filler", v }).ToArray())));
#pragma warning restore CA2000

        accessor = new BenchmarkAccessor();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        mockInt.Dispose();
        mockWide.Dispose();
        mockExtraWide.Dispose();
        mockExtraWideUnmapped.Dispose();
    }

    // ----- 内部対照：生成コードがどの設定でも同一 -----

    [Benchmark(Baseline = true, Description = "control Narrow1 Generated", OperationsPerInvoke = Iterations)]
    [BenchmarkCategory("Control")]
    public int ControlNarrowGenerated()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += accessor.QueryInt(mockInt).Count;
        }

        return total;
    }

    // ----- 10 列：閾値 8 なら switch、閾値 16 なら直比較、HEAD なら FrozenDictionary -----

    [Benchmark(Baseline = true, Description = "Wide10 Manual", OperationsPerInvoke = Iterations)]
    [BenchmarkCategory("Wide10")]
    public int Wide10Manual()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += ManualMappers.QueryWide(mockWide).Count;
        }

        return total;
    }

    [Benchmark(Description = "Wide10 Generated", OperationsPerInvoke = Iterations)]
    [BenchmarkCategory("Wide10")]
    public int Wide10Generated()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += accessor.QueryWide(mockWide).Count;
        }

        return total;
    }

    // ----- 20 列：閾値 8 / 16 のどちらでも switch、HEAD なら FrozenDictionary -----

    [Benchmark(Baseline = true, Description = "ExtraWide20 Manual", OperationsPerInvoke = Iterations)]
    [BenchmarkCategory("ExtraWide20")]
    public int ExtraWide20Manual()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += ManualMappers.QueryExtraWide(mockExtraWide).Count;
        }

        return total;
    }

    [Benchmark(Description = "ExtraWide20 Generated", OperationsPerInvoke = Iterations)]
    [BenchmarkCategory("ExtraWide20")]
    public int ExtraWide20Generated()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += accessor.QueryExtraWide(mockExtraWide).Count;
        }

        return total;
    }

    // ----- 20 列＋未マップ 20 列：Manual(GetOrdinal) を同一リーダー形の baseline に立てる -----

    [Benchmark(Baseline = true, Description = "ExtraWide20+unmapped Manual", OperationsPerInvoke = Iterations)]
    [BenchmarkCategory("ExtraWide20Unmapped")]
    public int ExtraWide20UnmappedManual()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += ManualMappers.QueryExtraWide(mockExtraWideUnmapped).Count;
        }

        return total;
    }

    [Benchmark(Description = "ExtraWide20+unmapped Generated", OperationsPerInvoke = Iterations)]
    [BenchmarkCategory("ExtraWide20Unmapped")]
    public int ExtraWide20UnmappedGenerated()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            total += accessor.QueryExtraWide(mockExtraWideUnmapped).Count;
        }

        return total;
    }
}
#pragma warning restore CA1001
