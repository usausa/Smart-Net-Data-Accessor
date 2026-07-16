namespace Smart.Data.Accessor.Benchmark;

using System.Collections.Frozen;
using System.Data.Common;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

using Smart.Mock.Data;

// 序数解決の narrow エンティティ(グループ数 1〜2)特化 PoC：現行の FrozenDictionary 形と、閾値分岐案の
// String.Equals 直比較形を __From 相当部分だけで比較する(クエリ毎 1 回のコスト)。列数の多い結果セットに
// narrow エンティティを合わせるケース(部分列)も含む。判断結果は __docs/benchmark-results.md に記録する。
//
// Run: dotnet run -c Release --project Smart.Data.Accessor.Benchmark -- --filter *NarrowOrdinal*
#pragma warning disable CA1001
[Config(typeof(MappingConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class NarrowOrdinalResolutionBenchmark
{
    // 現行 emit と同じ形の static FrozenDictionary(OrdinalIgnoreCase、列名→グループ id)。
    private static readonly FrozenDictionary<string, int> OneColumn = FrozenDictionary.ToFrozenDictionary(
        new Dictionary<string, int>(1, StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = 0,
        },
        StringComparer.OrdinalIgnoreCase);

    private static readonly FrozenDictionary<string, int> TwoColumns = FrozenDictionary.ToFrozenDictionary(
        new Dictionary<string, int>(2, StringComparer.OrdinalIgnoreCase)
        {
            ["Id"] = 0,
            ["Name"] = 1,
        },
        StringComparer.OrdinalIgnoreCase);

    private MockDataReader readerExact1 = default!;
    private MockDataReader readerExact2 = default!;
    private MockDataReader readerWideFirst = default!;
    private MockDataReader readerWideLast = default!;

    [GlobalSetup]
    public void Setup()
    {
        // GetName/FieldCount しか使わないため、リーダーは 1 度作って再利用できる(Read で状態が進まない)。
        readerExact1 = new MockDataReader([new MockColumn(typeof(long), "Id")], []);
        readerExact2 = new MockDataReader(
            [new MockColumn(typeof(long), "Id"), new MockColumn(typeof(string), "Name")], []);
        readerWideFirst = new MockDataReader(
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
            []);
        readerWideLast = new MockDataReader(
            [
                new MockColumn(typeof(int), "Age"),
                new MockColumn(typeof(double), "Score"),
                new MockColumn(typeof(bool), "Active"),
                new MockColumn(typeof(int), "Status"),
                new MockColumn(typeof(string), "Description"),
                new MockColumn(typeof(int), "Category"),
                new MockColumn(typeof(string), "Tag"),
                new MockColumn(typeof(double), "Weight"),
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name")
            ],
            []);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        readerExact1.Dispose();
        readerExact2.Dispose();
        readerWideFirst.Dispose();
        readerWideLast.Dispose();
    }

    // ----- 1 group -----

    [Benchmark(Baseline = true, Description = "Frozen (現行)")]
    [BenchmarkCategory("1col exact")]
    public int OneExactFrozen() => FrozenOne(readerExact1);

    [Benchmark(Description = "Direct (String.Equals)")]
    [BenchmarkCategory("1col exact")]
    public int OneExactDirect() => DirectOne(readerExact1);

    [Benchmark(Baseline = true, Description = "Frozen (現行)")]
    [BenchmarkCategory("1col in wide10 (hit last)")]
    public int OneWideLastFrozen() => FrozenOne(readerWideLast);

    [Benchmark(Description = "Direct (String.Equals)")]
    [BenchmarkCategory("1col in wide10 (hit last)")]
    public int OneWideLastDirect() => DirectOne(readerWideLast);

    // ----- 2 groups -----

    [Benchmark(Baseline = true, Description = "Frozen (現行)")]
    [BenchmarkCategory("2col exact")]
    public int TwoExactFrozen() => FrozenTwo(readerExact2);

    [Benchmark(Description = "Direct (String.Equals)")]
    [BenchmarkCategory("2col exact")]
    public int TwoExactDirect() => DirectTwo(readerExact2);

    [Benchmark(Baseline = true, Description = "Frozen (現行)")]
    [BenchmarkCategory("2col in wide10 (hit first)")]
    public int TwoWideFirstFrozen() => FrozenTwo(readerWideFirst);

    [Benchmark(Description = "Direct (String.Equals)")]
    [BenchmarkCategory("2col in wide10 (hit first)")]
    public int TwoWideFirstDirect() => DirectTwo(readerWideFirst);

    [Benchmark(Baseline = true, Description = "Frozen (現行)")]
    [BenchmarkCategory("2col in wide10 (hit last)")]
    public int TwoWideLastFrozen() => FrozenTwo(readerWideLast);

    [Benchmark(Description = "Direct (String.Equals)")]
    [BenchmarkCategory("2col in wide10 (hit last)")]
    public int TwoWideLastDirect() => DirectTwo(readerWideLast);

    // ----- 実装(現行 emit / 閾値分岐案 emit の忠実再現) -----

    private static int FrozenOne(DbDataReader reader)
    {
        Span<int> ordinals = stackalloc int[1];
        ordinals.Fill(-1);
        var fieldCount = reader.FieldCount;
        for (var i = 0; i < fieldCount; i++)
        {
            if (OneColumn.TryGetValue(reader.GetName(i), out var index) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                // emit の形は resolved カウンタ＋ `if (resolved == 1) break;` だが、グループ数 1 では常に真
                // (CA1508)のため無条件 break とする(JIT 畳み込み後は同一)。
                break;
            }
        }
        return ordinals[0];
    }

    private static (int Id, int Name) FrozenTwoCore(DbDataReader reader)
    {
        Span<int> ordinals = stackalloc int[2];
        ordinals.Fill(-1);
        var resolved = 0;
        var fieldCount = reader.FieldCount;
        for (var i = 0; i < fieldCount; i++)
        {
            if (TwoColumns.TryGetValue(reader.GetName(i), out var index) && (ordinals[index] < 0))
            {
                ordinals[index] = i;
                resolved++;
                if (resolved == 2)
                {
                    break;
                }
            }
        }
        return (ordinals[0], ordinals[1]);
    }

    private static int FrozenTwo(DbDataReader reader)
    {
        var (id, name) = FrozenTwoCore(reader);
        return id + name;
    }

    private static int DirectOne(DbDataReader reader)
    {
        var fieldCount = reader.FieldCount;
        for (var i = 0; i < fieldCount; i++)
        {
            if (String.Equals(reader.GetName(i), "Id", StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private static int DirectTwo(DbDataReader reader)
    {
        var ord0 = -1;
        var ord1 = -1;
        var fieldCount = reader.FieldCount;
        for (var i = 0; i < fieldCount; i++)
        {
            var name = reader.GetName(i);
            if ((ord0 < 0) && String.Equals(name, "Id", StringComparison.OrdinalIgnoreCase))
            {
                ord0 = i;
                if (ord1 >= 0)
                {
                    break;
                }
            }
            else if ((ord1 < 0) && String.Equals(name, "Name", StringComparison.OrdinalIgnoreCase))
            {
                ord1 = i;
                if (ord0 >= 0)
                {
                    break;
                }
            }
        }
        return ord0 + ord1;
    }
}
#pragma warning restore CA1001
