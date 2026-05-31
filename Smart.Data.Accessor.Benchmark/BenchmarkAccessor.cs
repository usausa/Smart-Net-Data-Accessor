namespace Smart.Data.Accessor.Benchmark;

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet requires accessor type referenced from benchmark methods to be public.")]
public sealed partial class BenchmarkAccessor
{
    [Query]
    public partial IReadOnlyList<BenchIntRow> QueryInt(DbConnection con);

    [Query]
    public partial IReadOnlyList<BenchWideRow> QueryWide(DbConnection con);

    [Query]
    public partial IReadOnlyList<BenchEnumRow> QueryWithEnum(DbConnection con);
}
