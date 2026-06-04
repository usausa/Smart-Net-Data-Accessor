namespace Example.ConsoleApplication.Accessor;

using System.Data;
using System.Data.Common;

using Example.ConsoleApplication.Models;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
internal sealed partial class ExampleAccessor
{
    [Execute]
    public partial int Create();

    [Insert(typeof(DataEntity), Table = "Data")]
    [Execute]
    public partial int Insert(DataEntity entity);

    [Query]
    public partial IReadOnlyList<DataEntity> QueryDataList();

    [Query]
    public partial IReadOnlyList<DataEntity> QueryByType(
        [DbType<DbType>(DbType.Int32)] int type);

    [Query]
    public partial IReadOnlyList<DataEntity> QueryAllWithConnection(DbConnection conn);

    [Execute]
    public partial int InsertNameByConnection(DbConnection conn, string name, int type);

    [Execute]
    public partial int InsertNameByTransaction(DbTransaction tx, string name, int type);

    [Select(typeof(DataEntity), Table = "Data")]
    [Query]
    public partial IReadOnlyList<DataEntity> SelectAll();

    [Update(typeof(DataEntity), Table = "Data")]
    [Execute]
    public partial int UpdateEntity(DataEntity entity);

    [Delete(typeof(DataEntity), Table = "Data")]
    [Execute]
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
    public partial int ExecuteDirect(string sql, [Direction(ParameterDirection.Output)] out int rows);

    [Count(typeof(DataEntity), Table = "Data")]
    [ExecuteScalar]
    public partial long CountAll();

    // Custom [TypeHandler] sample (F6): EventEntity.OccurredAt is a DateTime stored as Int64 ticks.
    [Execute]
    public partial int CreateEvents();

    // Builder INSERT (entity mode) — OccurredAt is written via DateTimeToTicksConverter.ToDb.
    [Insert(typeof(EventEntity), Table = "Events")]
    [Execute]
    public partial int InsertEvent(EventEntity entity);

    // OccurredAt is read back via DateTimeToTicksConverter.FromDb.
    [Query]
    public partial IReadOnlyList<EventEntity> QueryEvents();
}
