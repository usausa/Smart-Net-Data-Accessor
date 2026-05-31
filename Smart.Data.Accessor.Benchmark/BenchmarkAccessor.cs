namespace Smart.Data.Accessor.Benchmark;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet requires accessor type referenced from benchmark methods to be public.")]
public sealed partial class BenchmarkAccessor
{
    [Execute]
    public partial int CreateTable();

    [Execute]
    public partial int InsertRow(long id, string name, int age, double score, bool active, int status, string description, int category, string tag, double weight);

    [Query]
    public partial IReadOnlyList<BenchIntRow> QueryInt();

    [Query]
    public partial IReadOnlyList<BenchWideRow> QueryWide();

    [Query]
    public partial IReadOnlyList<BenchEnumRow> QueryWithEnum();
}
