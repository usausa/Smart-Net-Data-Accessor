namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class ExecuteAccessor
{
    [Execute]
    public partial int InsertName(DbConnection con, string name, int type);

    [Execute]
    public partial void DeleteById(DbConnection con, long id);

    [ExecuteScalar]
    public partial long CountAll(DbConnection con);

    [ExecuteReader]
    public partial DbDataReader ReadAll(DbConnection con);

    [ExecuteReader]
    public partial Task<DbDataReader> ReadAllAsync(DbConnection con, CancellationToken cancel = default);

    [Query]
    public partial IReadOnlyList<DataRecord> QueryRecords(DbConnection con);
}
