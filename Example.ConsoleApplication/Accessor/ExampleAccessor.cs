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
    public partial IReadOnlyList<DataEntity> QueryByKind(DataType kind);

    [Query]
    public partial IReadOnlyList<DataRecord> QueryAllAsRecord();

    [Query]
    public partial IReadOnlyList<DataEntity> QueryByIds(IEnumerable<long> ids);

    // [ReaderBehavior]: reader 形は列の読み順を呼出側が制御するため SequentialAccess を安全にオプトインできる。
    [ExecuteReader]
    [ReaderBehavior(CommandBehavior.SequentialAccess)]
    public partial DbDataReader QueryReader();

    [DirectSql]
    [Execute]
    public partial int ExecuteDirect(string sql, [Direction(ParameterDirection.Output)] out int rows);

    // Inline 2-way SQL (F19): SQL テキストを属性コンストラクタに記述し、.sql ファイルと同じパイプラインで処理する。
    [Query]
    [Sql("SELECT * FROM Data WHERE Type >= /*@ minType */0 ORDER BY Id")]
    public partial IReadOnlyList<DataEntity> QueryByTypeInline(int minType);

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
