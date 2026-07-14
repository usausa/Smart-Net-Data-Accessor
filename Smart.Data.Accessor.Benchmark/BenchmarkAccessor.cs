namespace Smart.Data.Accessor.Benchmark;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
public sealed partial class BenchmarkAccessor
{
    [Query]
    public partial IReadOnlyList<BenchIntRow> QueryInt(DbConnection con);

    [Query]
    public partial IReadOnlyList<BenchWideRow> QueryWide(DbConnection con);

    [Query]
    public partial IReadOnlyList<BenchWideRecord> QueryWideRecord(DbConnection con);

    [Query]
    public partial IReadOnlyList<BenchEnumRow> QueryWithEnum(DbConnection con);

    [Query]
    public partial IReadOnlyList<BenchTicksRow> QueryTicks(DbConnection con);

    // ExecuteHelper path benchmarks: IN-list expansion (dynamic SQL), scalar coercion,
    // multi-parameter binding, enum member-path binding and the GetValue<T> fallback.

    [Execute]
    public partial int DeleteByIds(DbConnection con, List<int> ids);

    [ExecuteScalar]
    public partial long CountLong(DbConnection con);

    [ExecuteScalar]
    public partial int CountInt(DbConnection con);

    [ExecuteScalar]
    public partial int? CountNullableInt(DbConnection con);

    [Execute]
    public partial int InsertWide(DbConnection con, long id, string name, int age, double score, bool active, int status, string description, int category, string tag, double weight);

    [Execute]
    public partial int UpdateStatus(DbConnection con, BenchEnumRow row);

    [Query]
    public partial IReadOnlyList<BenchUIntRow> QueryUInt(DbConnection con);
}
