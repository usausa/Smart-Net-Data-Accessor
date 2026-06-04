namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class BuilderAccessor
{
    [Insert(typeof(DataEntity), Table = "Data")]
    [Execute]
    public partial int Insert(DbConnection con, DataEntity entity);

    [Update(typeof(DataEntity), Table = "Data")]
    [Execute]
    public partial int Update(DbConnection con, DataEntity entity);

    [Delete(typeof(DataEntity), Table = "Data")]
    [Execute]
    public partial int DeleteById(DbConnection con, long id);

    [Count(typeof(DataEntity), Table = "Data")]
    [ExecuteScalar]
    public partial long CountAll(DbConnection con);

    [Select(typeof(DataEntity), Table = "Data")]
    [Query]
    public partial IReadOnlyList<DataEntity> SelectAll(DbConnection con);

    [SelectSingle(typeof(DataEntity), Table = "Data")]
    [QueryFirst]
    public partial DataEntity? Find(DbConnection con, long id);

    // Parameter mode (no entity type): columns derived from the value parameters.
    [Insert(Table = "Data")]
    [Execute]
    public partial int InsertRaw(DbConnection con, int id, string name);
}
