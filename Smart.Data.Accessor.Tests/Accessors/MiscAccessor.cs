namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
internal sealed partial class MiscAccessor
{
    [Execute]
    public partial int InsertByTx(DbTransaction tx, string name, int type);

    [Execute]
    public partial int InsertAnsi(DbConnection con, [DbType<DbType>(DbType.AnsiString)] string name);
}
