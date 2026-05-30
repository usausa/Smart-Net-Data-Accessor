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
    public partial IReadOnlyList<DataEntity> QueryByType(
        [DbType<System.Data.DbType>(System.Data.DbType.Int32)] int type);

    [Query]
    public partial IReadOnlyList<DataEntity> QueryAllWithConnection(DbConnection conn);

    [Execute]
    public partial int InsertNameByConnection(DbConnection conn, string name, int type);

    [Execute]
    public partial int InsertNameByTransaction(DbTransaction tx, string name, int type);

    [Query(Builder = nameof(BuildSelectAll))]
    public partial IReadOnlyList<DataEntity> SelectAll();

    [Execute(Builder = nameof(BuildUpdate))]
    public partial int UpdateEntity(DataEntity entity);

    [Execute(Builder = nameof(BuildDelete))]
    public partial int DeleteById(long id);

    [Query]
    public partial IReadOnlyList<DataEntity> QueryByKind(DataKind kind);

    [Query]
    public partial IReadOnlyList<DataRecord> QueryAllAsRecord();

    [Query]
    public partial IReadOnlyList<DataEntity> QueryByIds(IEnumerable<long> ids);

    [ExecuteReader]
    public partial DbDataReader QueryReader();

    [DirectSql]
    public partial int ExecuteDirect(string sql, [Direction(System.Data.ParameterDirection.Output)] out int rows);

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
