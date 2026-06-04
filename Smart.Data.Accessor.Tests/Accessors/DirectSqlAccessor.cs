namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
internal sealed partial class DirectSqlAccessor
{
    // SuppressWarning: this raw-SQL test intentionally opts out of the SDA0202 injection advisory.
    [DirectSql(SuppressWarning = true)]
    public partial int ExecRaw(DbConnection con, string sql);
}
