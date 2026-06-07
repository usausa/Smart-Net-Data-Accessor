namespace Smart.Data.Accessor.Builders.MySql.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.MySql.Generator.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

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
            case MySqlInsertModel model:
                EmitInsert(builder, model);
                break;
            case MySqlUpdateModel model:
                EmitUpdate(builder, model);
                break;
            case MySqlDeleteModel model:
                EmitDelete(builder, model);
                break;
            case MySqlCountModel model:
                SqlEmit.EmitCommandText(builder, "SELECT COUNT(*) FROM " + Quote(model.TableName));
                break;
            case MySqlTruncateModel model:
                SqlEmit.EmitCommandText(builder, "TRUNCATE TABLE " + Quote(model.TableName));
                break;
            case MySqlSelectModel model:
                EmitSelect(builder, model);
                break;
            case MySqlSelectSingleModel model:
                EmitSelectSingle(builder, model);
                break;
            case MySqlUpsertModel model:
                EmitUpsert(builder, model);
                break;
            case MySqlReplaceModel model:
                EmitReplace(builder, model);
                break;
            case MySqlInsertIgnoreModel model:
                EmitInsertIgnore(builder, model);
                break;
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT を組み立てる（INSERT INTO）。
    // Build an INSERT (INSERT INTO).
    private static void EmitInsert(SourceBuilder builder, MySqlInsertModel model)
        => EmitInsertForm(builder, "INSERT INTO", model, model.TableName, model.Columns, model.EntityParamName);

    // REPLACE INTO を組み立てる（列・値は INSERT と同形）。
    // Build REPLACE INTO (same column/value shape as INSERT).
    private static void EmitReplace(SourceBuilder builder, MySqlReplaceModel model)
        => EmitInsertForm(builder, "REPLACE INTO", model, model.TableName, model.Columns, model.EntityParamName);

    // INSERT IGNORE INTO を組み立てる（列・値は INSERT と同形）。
    // Build INSERT IGNORE INTO (same column/value shape as INSERT).
    private static void EmitInsertIgnore(SourceBuilder builder, MySqlInsertIgnoreModel model)
        => EmitInsertForm(builder, "INSERT IGNORE INTO", model, model.TableName, model.Columns, model.EntityParamName);

    // INSERT 系（INSERT INTO / REPLACE INTO / INSERT IGNORE INTO）の列・値・束縛を共通で組み立てる。verb は INTO までの先頭句。
    // エンティティモードは非 [DatabaseManaged] 列、パラメータモードはバインドパラメータを列・値にする。
    // Build the shared columns / values / bindings for the INSERT family (INSERT INTO / REPLACE INTO / INSERT IGNORE
    // INTO); verb is the leading clause through INTO. Entity mode uses the non-[DatabaseManaged] columns, parameter mode
    // uses the bind parameters.
    private static void EmitInsertForm(SourceBuilder builder, string verb, BuilderMethodModel model, string tableName, EquatableArray<BuilderColumn> columns, string? entityParamName)
    {
        if (entityParamName is not null)
        {
            var insertColumns = columns.Where(static x => !x.IsDatabaseManaged).ToList();
            var colSql = String.Join(", ", insertColumns.Select(x => Quote(x.ColumnName)));
            var valSql = String.Join(", ", insertColumns.Select(x => SqlEmit.Marker + x.PropertyName));
            SqlEmit.EmitCommandText(builder, $"{verb} {Quote(tableName)} ({colSql}) VALUES ({valSql})");
            foreach (var column in insertColumns)
            {
                SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + column.PropertyName, $"{entityParamName}.{column.PropertyName}", column);
            }
        }
        else
        {
            var bindParams = SqlEmit.BindParams(model);
            var colSql = String.Join(", ", bindParams.Select(x => Quote(x.ColumnName)));
            var valSql = String.Join(", ", bindParams.Select(x => SqlEmit.Marker + x.Name));
            SqlEmit.EmitCommandText(builder, $"{verb} {Quote(tableName)} ({colSql}) VALUES ({valSql})");
            foreach (var parameter in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, parameter);
            }
        }
    }

    // INSERT ... ON DUPLICATE KEY UPDATE を組み立てる。INSERT 列は非 [DatabaseManaged]、更新は非キー・非 [DatabaseManaged] 列を
    // `col = VALUES(col)` で。更新対象が無ければ全列を更新（最低 1 つの代入が要るため）。
    // Build INSERT ... ON DUPLICATE KEY UPDATE. INSERT columns are non-[DatabaseManaged]; the update assigns the
    // non-key, non-[DatabaseManaged] columns via `col = VALUES(col)`, falling back to all columns when there is nothing
    // else to update (the clause requires at least one assignment).
    private static void EmitUpsert(SourceBuilder builder, MySqlUpsertModel model)
    {
        if (!model.HasEntityType || (model.EntityParamName is null))
        {
            // エンティティ実体が無く列を解決できない場合は何も組み立てない（診断は transform 側で報告済み）。
            // Without an entity instance the column list is unresolved; emit nothing (the diagnostic is raised in the transform).
            return;
        }

        var columns = model.Columns.Where(static x => !x.IsDatabaseManaged).ToList();
        var updates = model.Columns.Where(static x => !x.IsKey && !x.IsDatabaseManaged).ToList();
        if (updates.Count == 0)
        {
            updates = columns;
        }

        var colSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
        var valSql = String.Join(", ", columns.Select(x => SqlEmit.Marker + x.PropertyName));
        var updateSql = String.Join(", ", updates.Select(x => $"{Quote(x.ColumnName)} = VALUES({Quote(x.ColumnName)})"));
        SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({colSql}) VALUES ({valSql}) ON DUPLICATE KEY UPDATE {updateSql}");

        foreach (var column in columns)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
    }

    // UPDATE を組み立てる。SET 句は非キーかつ非 [DatabaseManaged] 列、WHERE 句は [Key] 列（@k_ 接頭辞のパラメータ）。
    // エンティティが無い場合は "UPDATE T SET " だけを出力する。
    // Build an UPDATE: the SET clause uses non-key, non-[DatabaseManaged] columns; the WHERE clause uses [Key] columns
    // (parameters prefixed @k_). Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, MySqlUpdateModel model)
    {
        if (!model.HasEntityType || (model.EntityParamName is null))
        {
            SqlEmit.EmitCommandText(builder, "UPDATE " + Quote(model.TableName) + " SET ");
            return;
        }

        var columns = model.Columns;
        var settable = columns.Where(static x => !x.IsKey && !x.IsDatabaseManaged).ToList();
        var keys = columns.Where(static x => x.IsKey).ToList();

        var sql = new StringBuilder();
        sql.Append("UPDATE ").Append(Quote(model.TableName)).Append(" SET ");
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

        foreach (var column in settable)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
        foreach (var column in keys)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + "k_" + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
    }

    // DELETE を組み立てる。WHERE 句はバインドパラメータ（先頭から [Key] 列に対応付け）。
    // Build a DELETE: the WHERE clause uses the bind parameters (mapped to the key columns in order).
    private static void EmitDelete(SourceBuilder builder, MySqlDeleteModel model)
    {
        var keyColumns = model.Columns.Where(static x => x.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(model);

        var sql = new StringBuilder();
        sql.Append("DELETE FROM ").Append(Quote(model.TableName));
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

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter);
        }
    }

    // SELECT（全件）を組み立てる。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があればプロバイダのページング句を付ける。
    // Build a SELECT (all rows): SELECT * when there is no entity; otherwise list the columns and, if [Limit]/[Offset]
    // are present, append the provider's paging clause.
    private static void EmitSelect(SourceBuilder builder, MySqlSelectModel model)
    {
        if (!model.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Quote(model.TableName));
            return;
        }

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(String.Join(", ", model.Columns.Select(x => Quote(x.ColumnName)))).Append(" FROM ").Append(Quote(model.TableName));

        // [Limit]/[Offset] パラメータがある場合のみ、プロバイダのページング句を付加する（パラメータ束縛は offset→limit の順）。
        // Append the provider's paging clause only when [Limit]/[Offset] parameters are present (params bound offset-then-limit).
        var valueParams = model.ValueParams;
        var limitParam = valueParams.FirstOrDefault(static x => x.IsLimit);
        var offsetParam = valueParams.FirstOrDefault(static x => x.IsOffset);
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
    private static void EmitSelectSingle(SourceBuilder builder, MySqlSelectSingleModel model)
    {
        if (!model.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Quote(model.TableName));
            return;
        }

        var keyColumns = model.Columns.Where(static x => x.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(model);

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(String.Join(", ", model.Columns.Select(x => Quote(x.ColumnName)))).Append(" FROM ").Append(Quote(model.TableName));
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

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter);
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
