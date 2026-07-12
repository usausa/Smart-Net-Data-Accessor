namespace Smart.Data.Accessor.Benchmark;

using BenchmarkDotNet.Attributes;

using Smart.Mock.Data;

// Benchmarks for the ExecuteHelper runtime paths: IN-list expansion (AddInParameters, dynamic SQL),
// scalar coercion (ConvertScalar fast/slow path), multi-parameter binding (AssignValue),
// enum member-path binding and the GetValue<T> typed-reader fallback.
//
// Run: dotnet run -c Release --project Smart.Data.Accessor.Benchmark -- --filter *ExecuteHelperBenchmark*
#pragma warning disable CA1001
[Config(typeof(BenchmarkConfig))]
public class ExecuteHelperBenchmark
{
    private const int RowCount = 100;

    private MockRepeatDbConnection mockExecute = default!;
    private MockRepeatDbConnection mockScalar = default!;
    private MockRepeatDbConnection mockUInt = default!;

    private BenchmarkAccessor accessor = default!;

    private List<int> ids10 = default!;
    private List<int> ids100 = default!;
    private BenchEnumRow row = default!;

    [GlobalSetup]
    public void Setup()
    {
        mockExecute = new MockRepeatDbConnection(1);
        mockScalar = new MockRepeatDbConnection(42L);
#pragma warning disable CA2000
        mockUInt = new MockRepeatDbConnection(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(long), "Value")
            ],
            Enumerable.Range(1, RowCount).Select(static x => new object[]
            {
                (long)x,
                (long)(x % 1000)
            })));
#pragma warning restore CA2000

        accessor = new BenchmarkAccessor();
        ids10 = [.. Enumerable.Range(1, 10)];
        ids100 = [.. Enumerable.Range(1, 100)];
        row = new BenchEnumRow { Id = 1, Name = "Name", Status = BenchStatus.Active };
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        mockExecute.Dispose();
        mockScalar.Dispose();
        mockUInt.Dispose();
    }

    // -----------------------------------------------------------------
    // IN-list expansion (AddInParameters, dynamic SQL build)
    // -----------------------------------------------------------------

    [Benchmark(Description = "InClause: 10 ids")]
    public int InClause10() => accessor.DeleteByIds(mockExecute, ids10);

    [Benchmark(Description = "InClause: 100 ids")]
    public int InClause100() => accessor.DeleteByIds(mockExecute, ids100);

    // -----------------------------------------------------------------
    // Scalar coercion (ConvertScalar; the mock returns long)
    // -----------------------------------------------------------------

    [Benchmark(Description = "Scalar: long <- long (match)")]
    public long ScalarLongFromLong() => accessor.CountLong(mockScalar);

    [Benchmark(Description = "Scalar: int <- long (coerce)")]
    public int ScalarIntFromLong() => accessor.CountInt(mockScalar);

    [Benchmark(Description = "Scalar: int? <- long (coerce)")]
    public int? ScalarNullableIntFromLong() => accessor.CountNullableInt(mockScalar);

    // -----------------------------------------------------------------
    // Parameter binding (AssignValue per parameter)
    // -----------------------------------------------------------------

    [Benchmark(Description = "Bind: 10 parameters")]
    public int BindTenParameters() => accessor.InsertWide(mockExecute, 1L, "Name", 20, 1.5, true, 2, "Description", 3, "Tag", 0.25);

    [Benchmark(Description = "Bind: enum member path")]
    public int BindEnumMemberPath() => accessor.UpdateStatus(mockExecute, row);

    // -----------------------------------------------------------------
    // GetValue<T> typed-reader fallback (per row; the mock returns long for a uint property)
    // -----------------------------------------------------------------

    [Benchmark(Description = "GetValue fallback: uint <- long (100 rows)")]
    public IReadOnlyList<BenchUIntRow> QueryUIntFallback() => accessor.QueryUInt(mockUInt);
}
#pragma warning restore CA1001
