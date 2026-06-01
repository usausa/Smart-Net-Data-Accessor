namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
internal sealed partial class DirectSqlAccessor
{
    [DirectSql]
    public partial int ExecRaw(DbConnection con, string sql);
}
