namespace Smart.Data.Accessor.Tests.Accessors;

using System.Collections.Generic;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class DirectSqlAccessor
{
    [DirectSql]
    public partial int ExecRaw(DbConnection con, string sql);

    // DirectSql × Query: 生 SQL で行を取得しマッピングする（2 軸モデルで新規に成立する組み合わせ）。
    // DirectSql × Query: raw SQL that returns mapped rows (a combination newly valid under the two-axis model).
    [DirectSql]
    [Query]
    public partial IReadOnlyList<DataEntity> QueryRaw(DbConnection con, string sql);

    // DirectSql × ExecuteReader: 生 SQL でリーダーを返す。
    // DirectSql × ExecuteReader: raw SQL returning a reader.
    [DirectSql]
    [ExecuteReader]
    public partial DbDataReader ReadRaw(DbConnection con, string sql);
}
