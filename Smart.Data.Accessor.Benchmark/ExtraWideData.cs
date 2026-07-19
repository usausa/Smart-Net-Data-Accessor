namespace Smart.Data.Accessor.Benchmark;

using Smart.Mock.Data;

// ExtraWide 系ベンチ(ComparisonBenchmark / OrdinalStrategyBenchmark)が共有する 20 列のリーダー定義。
// クラス毎に私本を持つと列追加時に乖離する：名前照合マッピングは欠落列を default のまま許容するため、
// ベンチはエラーにならず異なる作業量を黙って測り続ける。列の増減は必ずここだけで行うこと
// (BenchExtraWideRow / ManualMappers.QueryExtraWide / ComparisonBenchmark.ExtraWideSql も連動)。
// The 20-column reader definition shared by the ExtraWide benchmarks (ComparisonBenchmark / OrdinalStrategyBenchmark).
// Per-class copies drift when a column is added: name matching tolerates a missing column, so the benchmark keeps
// running while silently measuring different work. Add or remove columns only here (BenchExtraWideRow /
// ManualMappers.QueryExtraWide / ComparisonBenchmark.ExtraWideSql track this list).
internal static class ExtraWideData
{
    public static MockColumn[] Columns() =>
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
        new MockColumn(typeof(int), "Owner"),
        new MockColumn(typeof(int), "Team"),
        new MockColumn(typeof(int), "Level"),
        new MockColumn(typeof(int), "Position"),
        new MockColumn(typeof(int), "Version"),
        new MockColumn(typeof(string), "City"),
        new MockColumn(typeof(string), "State"),
        new MockColumn(typeof(string), "Country"),
        new MockColumn(typeof(string), "Note"),
        new MockColumn(typeof(string), "Memo")
    ];

    public static object[] Values(int x) =>
    [
        (long)x, $"Name-{x}", x % 80, x * 1.5, (x % 2) == 0, x % 4, $"Description-{x}", x % 8, $"Tag-{x}", x * 0.25,
        x % 16, x % 32, x % 5, x % 7, x, $"City-{x}", $"State-{x}", $"Country-{x}", $"Note-{x}", $"Memo-{x}"
    ];
}
