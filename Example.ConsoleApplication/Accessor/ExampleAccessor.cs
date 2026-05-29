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

    [Query]
    public partial IReadOnlyList<DataEntity> QueryByType(int type);

    [Query(Builder = nameof(BuildSelectAll))]
    public partial IReadOnlyList<DataEntity> SelectAll();

    [Execute(Builder = nameof(BuildUpdate))]
    public partial int UpdateEntity(DataEntity entity);

    [Execute(Builder = nameof(BuildDelete))]
    public partial int DeleteById(long id);

    [ExecuteScalar(Builder = nameof(BuildCount))]
    public partial long CountAll();

    [InsertBuilder(typeof(DataEntity), Table = "Data")]
    private static partial void BuildInsert(ref BuilderContext ctx, DataEntity entity);

    [SelectBuilder(typeof(DataEntity), Table = "Data")]
    private static partial void BuildSelectAll(ref BuilderContext ctx);

    [UpdateBuilder(typeof(DataEntity), Table = "Data")]
    private static partial void BuildUpdate(ref BuilderContext ctx, DataEntity entity);

    [DeleteBuilder(typeof(DataEntity), Table = "Data")]
    private static partial void BuildDelete(ref BuilderContext ctx, long id);

    [CountBuilder(typeof(DataEntity), Table = "Data")]
    private static partial void BuildCount(ref BuilderContext ctx);
}
