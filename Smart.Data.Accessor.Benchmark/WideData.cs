namespace Smart.Data.Accessor.Benchmark;

using Smart.Mock.Data;

// wide 系ベンチ(QueryBenchmark / ComparisonBenchmark / OrdinalStrategyBenchmark)が共有する 10 列の
// リーダー定義。クラス毎に私本を持つと列追加時に乖離する：名前照合マッピングは欠落列を default のまま
// 許容するため、ベンチはエラーにならず異なる作業量を黙って測り続ける。列の増減は必ずここだけで行うこと
// (BenchWideRow / ManualMappers.QueryWide / ComparisonBenchmark.WideSql も連動)。
// The 10-column reader definition shared by the wide benchmarks (QueryBenchmark / ComparisonBenchmark /
// OrdinalStrategyBenchmark). Per-class copies drift when a column is added: name matching tolerates a missing
// column, so the benchmark keeps running while silently measuring different work. Add or remove columns only
// here (BenchWideRow / ManualMappers.QueryWide / ComparisonBenchmark.WideSql track this list).
internal static class WideData
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
        new(typeof(double), "Weight")
    ];

    public static object[] Values(int x) =>
    [
        (long)x, $"Name-{x}", x % 80, x * 1.5, (x % 2) == 0, x % 4, $"Description-{x}", x % 8, $"Tag-{x}", x * 0.25
    ];
}
