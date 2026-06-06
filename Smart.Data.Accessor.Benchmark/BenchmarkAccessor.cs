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
    public partial IReadOnlyList<BenchEnumRow> QueryWithEnum(DbConnection con);

    [Query]
    public partial IReadOnlyList<BenchTicksRow> QueryTicks(DbConnection con);
}
