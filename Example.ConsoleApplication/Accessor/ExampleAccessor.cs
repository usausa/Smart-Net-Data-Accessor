namespace Example.ConsoleApplication.Accessor;

using System.Collections.Generic;
using System.Data.Common;

using Example.ConsoleApplication.Models;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders;

[DataAccessor]
internal sealed partial class ExampleAccessor
{
    [Execute]
    public partial int Create();

    [Execute(Builder = nameof(BuildInsert))]
    public partial int Insert(DataEntity entity);

    [Query]
    public partial IReadOnlyList<DataEntity> QueryDataList();

    [InsertBuilder(typeof(DataEntity), Table = "Data")]
    private static partial void BuildInsert(BuilderContext ctx, DataEntity entity);
}
