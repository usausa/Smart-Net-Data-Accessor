namespace Smart.Data.Accessor.Builders.MySql.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.GeneratorShared.Engine;
using Smart.Data.Accessor.Builders.GeneratorShared.Models;
using Smart.Data.Accessor.Builders.MySql.Generator.Models;

using SourceGenerateHelper;

// MySQL Builder の emit：種別毎の SQL 組み立て（バッククォート、LIMIT/OFFSET、ON DUPLICATE KEY UPDATE / REPLACE / INSERT IGNORE）。
// Emit for the MySQL builder: per-kind SQL assembly (backtick quoting, LIMIT/OFFSET, ON DUPLICATE KEY UPDATE / REPLACE / INSERT IGNORE).
internal static class MySqlSourceBuilder
{
    // 1 メソッド分のヘルパーを出力する。シグネチャと cmd 取得・スコープ開閉は共有の SqlEmit、種別毎の本体はこのプロバイダーが持つ。
    // Emit one method's helper. The signature, cmd acquisition and scope open/close come from the shared SqlEmit; the
    // per-kind body is owned by this provider.
    public static void EmitMethod(SourceBuilder builder, BuilderMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method);

        switch (method)
        {
            case MySqlInsertModel m:
                EmitInsert(builder, m);
                break;
            case MySqlUpdateModel m:
                EmitUpdate(builder, m);
                break;
            case MySqlDeleteModel m:
                EmitDelete(builder, m);
                break;
            case MySqlCountModel m:
                SqlEmit.EmitCommandText(builder, "SELECT COUNT(*) FROM " + Quote(m.TableName));
                break;
            case MySqlTruncateModel m:
                SqlEmit.EmitCommandText(builder, "TRUNCATE TABLE " + Quote(m.TableName));
                break;
            case MySqlSelectModel m:
                EmitSelect(builder, m);
                break;
            case MySqlSelectSingleModel m:
                EmitSelectSingle(builder, m);
                break;
            case MySqlUpsertModel m:
                EmitUpsert(builder, m);
                break;
            case MySqlReplaceModel m:
                EmitReplace(builder, m);
                break;
            case MySqlInsertIgnoreModel m:
                EmitInsertIgnore(builder, m);
                break;
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT を組み立てる（INSERT INTO）。
    // Build an INSERT (INSERT INTO).
    private static void EmitInsert(SourceBuilder builder, MySqlInsertModel m)
        => EmitInsertForm(builder, "INSERT INTO", m, m.TableName, m.Columns, m.EntityParamName);

    // REPLACE INTO を組み立てる（列・値は INSERT と同形）。
    // Build REPLACE INTO (same column/value shape as INSERT).
    private static void EmitReplace(SourceBuilder builder, MySqlReplaceModel m)
        => EmitInsertForm(builder, "REPLACE INTO", m, m.TableName, m.Columns, m.EntityParamName);

    // INSERT IGNORE INTO を組み立てる（列・値は INSERT と同形）。
    // Build INSERT IGNORE INTO (same column/value shape as INSERT).
    private static void EmitInsertIgnore(SourceBuilder builder, MySqlInsertIgnoreModel m)
        => EmitInsertForm(builder, "INSERT IGNORE INTO", m, m.TableName, m.Columns, m.EntityParamName);

    // INSERT 系（INSERT INTO / REPLACE INTO / INSERT IGNORE INTO）の列・値・束縛を共通で組み立てる。verb は INTO までの先頭句。
    // エンティティモードは非 [DatabaseManaged] 列、パラメータモードはバインドパラメータを列・値にする。
    // Build the shared columns / values / bindings for the INSERT family (INSERT INTO / REPLACE INTO / INSERT IGNORE
    // INTO); verb is the leading clause through INTO. Entity mode uses the non-[DatabaseManaged] columns, parameter mode
    // uses the bind parameters.
    private static void EmitInsertForm(SourceBuilder builder, string verb, BuilderMethodModel model, string tableName, EquatableArray<BuilderColumn> columns, string? entityParamName)
    {
        if (entityParamName is not null)
        {
            var cols = columns.Where(static c => !c.IsDatabaseManaged).ToList();
            var colSql = String.Join(", ", cols.Select(c => Quote(c.ColumnName)));
            var valSql = String.Join(", ", cols.Select(c => SqlEmit.Marker + c.PropertyName));
            SqlEmit.EmitCommandText(builder, $"{verb} {Quote(tableName)} ({colSql}) VALUES ({valSql})");
            foreach (var c in cols)
            {
                SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + c.PropertyName, $"{entityParamName}.{c.PropertyName}", c);
            }
        }
        else
        {
            var bindParams = SqlEmit.BindParams(model);
            var colSql = String.Join(", ", bindParams.Select(p => Quote(p.ColumnName)));
            var valSql = String.Join(", ", bindParams.Select(p => SqlEmit.Marker + p.Name));
            SqlEmit.EmitCommandText(builder, $"{verb} {Quote(tableName)} ({colSql}) VALUES ({valSql})");
            foreach (var p in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, p);
            }
        }
    }

    // INSERT ... ON DUPLICATE KEY UPDATE を組み立てる。INSERT 列は非 [DatabaseManaged]、更新は非キー・非 [DatabaseManaged] 列を
    // `col = VALUES(col)` で。更新対象が無ければ全列を更新（最低 1 つの代入が要るため）。
    // Build INSERT ... ON DUPLICATE KEY UPDATE. INSERT columns are non-[DatabaseManaged]; the update assigns the
    // non-key, non-[DatabaseManaged] columns via `col = VALUES(col)`, falling back to all columns when there is nothing
    // else to update (the clause requires at least one assignment).
    private static void EmitUpsert(SourceBuilder builder, MySqlUpsertModel m)
    {
        if (!m.HasEntityType || (m.EntityParamName is null))
        {
            // エンティティ実体が無く列を解決できない場合は何も組み立てない（診断は transform 側で報告済み）。
            // Without an entity instance the column list is unresolved; emit nothing (the diagnostic is raised in the transform).
            return;
        }

        var cols = m.Columns.Where(static c => !c.IsDatabaseManaged).ToList();
        var updates = m.Columns.Where(static c => !c.IsKey && !c.IsDatabaseManaged).ToList();
        if (updates.Count == 0)
        {
            updates = cols;
        }

        var colSql = String.Join(", ", cols.Select(c => Quote(c.ColumnName)));
        var valSql = String.Join(", ", cols.Select(c => SqlEmit.Marker + c.PropertyName));
        var updateSql = String.Join(", ", updates.Select(c => $"{Quote(c.ColumnName)} = VALUES({Quote(c.ColumnName)})"));
        SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(m.TableName)} ({colSql}) VALUES ({valSql}) ON DUPLICATE KEY UPDATE {updateSql}");

        foreach (var c in cols)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + c.PropertyName, $"{m.EntityParamName}.{c.PropertyName}", c);
        }
    }

    // UPDATE を組み立てる。SET 句は非キーかつ非 [DatabaseManaged] 列、WHERE 句は [Key] 列（@k_ 接頭辞のパラメータ）。
    // エンティティが無い場合は "UPDATE T SET " だけを出力する。
    // Build an UPDATE: the SET clause uses non-key, non-[DatabaseManaged] columns; the WHERE clause uses [Key] columns
    // (parameters prefixed @k_). Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, MySqlUpdateModel m)
    {
        if (!m.HasEntityType || (m.EntityParamName is null))
        {
            SqlEmit.EmitCommandText(builder, "UPDATE " + Quote(m.TableName) + " SET ");
            return;
        }

        var columns = m.Columns;
        var settable = columns.Where(static c => !c.IsKey && !c.IsDatabaseManaged).ToList();
        var keys = columns.Where(static c => c.IsKey).ToList();

        var sql = new StringBuilder();
        sql.Append("UPDATE ").Append(Quote(m.TableName)).Append(" SET ");
        for (var i = 0; i < settable.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(Quote(settable[i].ColumnName)).Append(" = ").Append(SqlEmit.Marker).Append(settable[i].PropertyName);
        }
        if (keys.Count > 0)
        {
            sql.Append(" WHERE ");
            for (var i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(" AND ");
                }
                sql.Append(Quote(keys[i].ColumnName)).Append(" = ").Append(SqlEmit.Marker).Append("k_").Append(keys[i].PropertyName);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var c in settable)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + c.PropertyName, $"{m.EntityParamName}.{c.PropertyName}", c);
        }
        foreach (var c in keys)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + "k_" + c.PropertyName, $"{m.EntityParamName}.{c.PropertyName}", c);
        }
    }

    // DELETE を組み立てる。WHERE 句はバインドパラメータ（先頭から [Key] 列に対応付け）。
    // Build a DELETE: the WHERE clause uses the bind parameters (mapped to the key columns in order).
    private static void EmitDelete(SourceBuilder builder, MySqlDeleteModel m)
    {
        var keyColumns = m.Columns.Where(static c => c.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(m);

        var sql = new StringBuilder();
        sql.Append("DELETE FROM ").Append(Quote(m.TableName));
        if (bindParams.Count > 0)
        {
            sql.Append(" WHERE ");
            for (var i = 0; i < bindParams.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(" AND ");
                }
                var column = i < keyColumns.Count ? keyColumns[i].ColumnName : bindParams[i].ColumnName;
                sql.Append(Quote(column)).Append(" = ").Append(SqlEmit.Marker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var p in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, p);
        }
    }

    // SELECT（全件）を組み立てる。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があればプロバイダのページング句を付ける。
    // Build a SELECT (all rows): SELECT * when there is no entity; otherwise list the columns and, if [Limit]/[Offset]
    // are present, append the provider's paging clause.
    private static void EmitSelect(SourceBuilder builder, MySqlSelectModel m)
    {
        if (!m.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Quote(m.TableName));
            return;
        }

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(String.Join(", ", m.Columns.Select(c => Quote(c.ColumnName)))).Append(" FROM ").Append(Quote(m.TableName));

        // [Limit]/[Offset] パラメータがある場合のみ、プロバイダのページング句を付加する（パラメータ束縛は offset→limit の順）。
        // Append the provider's paging clause only when [Limit]/[Offset] parameters are present (params bound offset-then-limit).
        var valueParams = m.ValueParams;
        var limitParam = valueParams.FirstOrDefault(static p => p.IsLimit);
        var offsetParam = valueParams.FirstOrDefault(static p => p.IsOffset);
        if ((limitParam is not null) || (offsetParam is not null))
        {
            AppendPaging(
                sql,
                limitParam is null ? null : SqlEmit.Marker + limitParam.Name,
                offsetParam is null ? null : SqlEmit.Marker + offsetParam.Name);
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        if (offsetParam is not null)
        {
            SqlEmit.EmitValueParamBinding(builder, offsetParam);
        }
        if (limitParam is not null)
        {
            SqlEmit.EmitValueParamBinding(builder, limitParam);
        }
    }

    // SELECT（単一行）を組み立てる。WHERE 句は [Key] 列に対応するバインドパラメータ。
    // Build a SELECT (single row): the WHERE clause uses bind parameters mapped to the [Key] columns.
    private static void EmitSelectSingle(SourceBuilder builder, MySqlSelectSingleModel m)
    {
        if (!m.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Quote(m.TableName));
            return;
        }

        var keyColumns = m.Columns.Where(static c => c.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(m);

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(String.Join(", ", m.Columns.Select(c => Quote(c.ColumnName)))).Append(" FROM ").Append(Quote(m.TableName));
        if (bindParams.Count > 0)
        {
            sql.Append(" WHERE ");
            for (var i = 0; i < bindParams.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(" AND ");
                }
                var column = i < keyColumns.Count ? keyColumns[i].ColumnName : bindParams[i].ColumnName;
                sql.Append(Quote(column)).Append(" = ").Append(SqlEmit.Marker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var p in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, p);
        }
    }

    // 識別子をバッククォートでクォートする（バッククォートは 2 重化してエスケープ）。
    // Quote an identifier with backticks (a backtick is escaped by doubling it).
    private static string Quote(string identifier) => "`" + identifier.Replace("`", "``") + "`";

    // LIMIT/OFFSET ページングを付加する。MySQL は OFFSET 単独に LIMIT が要るため、その場合は最大行数センチネルを補う。
    // Append LIMIT/OFFSET paging. MySQL requires a LIMIT for a bare OFFSET, so supply the documented max-row sentinel then.
    private static void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker)
    {
        if (limitMarker is not null)
        {
            sql.Append(" LIMIT ").Append(limitMarker);
            if (offsetMarker is not null)
            {
                sql.Append(" OFFSET ").Append(offsetMarker);
            }
        }
        else if (offsetMarker is not null)
        {
            sql.Append(" LIMIT 18446744073709551615 OFFSET ").Append(offsetMarker);
        }
    }
}
