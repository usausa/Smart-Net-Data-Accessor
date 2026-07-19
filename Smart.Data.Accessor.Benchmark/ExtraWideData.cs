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
        new(typeof(long), "Id"),
        new(typeof(string), "Name"),
        new(typeof(int), "Age"),
        new(typeof(double), "Score"),
        new(typeof(bool), "Active"),
        new(typeof(int), "Status"),
        new(typeof(string), "Description"),
        new(typeof(int), "Category"),
        new(typeof(string), "Tag"),
        new(typeof(double), "Weight"),
        new(typeof(int), "Owner"),
        new(typeof(int), "Team"),
        new(typeof(int), "Level"),
        new(typeof(int), "Position"),
        new(typeof(int), "Version"),
        new(typeof(string), "City"),
        new(typeof(string), "State"),
        new(typeof(string), "Country"),
        new(typeof(string), "Note"),
        new(typeof(string), "Memo")
    ];

    public static object[] Values(int x) =>
    [
        (long)x, $"Name-{x}", x % 80, x * 1.5, (x % 2) == 0, x % 4, $"Description-{x}", x % 8, $"Tag-{x}", x * 0.25,
        x % 16, x % 32, x % 5, x % 7, x, $"City-{x}", $"State-{x}", $"Country-{x}", $"Note-{x}", $"Memo-{x}"
    ];
}
