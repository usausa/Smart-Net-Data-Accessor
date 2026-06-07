namespace Smart.Data.Accessor.Builders.MySql.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.MySql.Generator.Models;
using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// MySQL Builder の emit：種別毎の SQL 組み立て（バッククォート、LIMIT/OFFSET、ON DUPLICATE KEY UPDATE / REPLACE / INSERT IGNORE）。
// Emit for the MySQL builder: per-kind SQL assembly (backtick quoting, LIMIT/OFFSET, ON DUPLICATE KEY UPDATE / REPLACE / INSERT IGNORE).
internal static class MySqlSourceBuilder
{
    public static void EmitMethod(SourceBuilder builder, MySqlMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method.MethodName, method.ValueParams);

        switch (method)
        {
            case MySqlInsertModel model:
                EmitInsertForm(builder, "INSERT INTO", model, model.TableName, model.Columns, model.EntityParamName);
                break;
            case MySqlReplaceModel model:
                EmitInsertForm(builder, "REPLACE INTO", model, model.TableName, model.Columns, model.EntityParamName);
                break;
            case MySqlInsertIgnoreModel model:
                EmitInsertForm(builder, "INSERT IGNORE INTO", model, model.TableName, model.Columns, model.EntityParamName);
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
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT 系（INSERT INTO / REPLACE INTO / INSERT IGNORE INTO）の列・値・束縛を共通で組み立てる。verb は INTO までの先頭句。
    // エンティティモードは非 [DatabaseManaged] 列、パラメータモードはバインドパラメータを列・値にする。
    // Build the shared columns / values / bindings for the INSERT family; verb is the leading clause through INTO.
    // Entity mode uses the non-[DatabaseManaged] columns, parameter mode uses the bind parameters.
    private static void EmitInsertForm(SourceBuilder builder, string verb, MySqlMethodModel model, string tableName, EquatableArray<ColumnBinding> columns, string? entityParamName)
    {
        if (entityParamName is not null)
        {
            var insertColumns = columns.Where(static x => !x.Flags.IsDatabaseManaged()).ToList();
            var columnSql = String.Join(", ", insertColumns.Select(x => Quote(x.ColumnName)));
            var valueSql = String.Join(", ", insertColumns.Select(x => model.BindMarker + x.PropertyName));
            SqlEmit.EmitCommandText(builder, $"{verb} {Quote(tableName)} ({columnSql}) VALUES ({valueSql})");
            foreach (var column in insertColumns)
            {
                SqlEmit.EmitColumnParameter(builder, model.BindMarker + column.PropertyName, $"{entityParamName}.{column.PropertyName}", column);
            }
        }
        else
        {
            var bindParams = SqlEmit.BindParams(model.ValueParams);
            var columnSql = String.Join(", ", bindParams.Select(x => Quote(x.ColumnName)));
            var valueSql = String.Join(", ", bindParams.Select(x => model.BindMarker + x.Name));
            SqlEmit.EmitCommandText(builder, $"{verb} {Quote(tableName)} ({columnSql}) VALUES ({valueSql})");
            foreach (var parameter in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
            }
        }
    }

    // INSERT ... ON DUPLICATE KEY UPDATE を組み立てる。更新は非キー・非 [DatabaseManaged] 列を `col = VALUES(col)` で。更新対象が無ければ全列。
    // Build INSERT ... ON DUPLICATE KEY UPDATE. The update assigns the non-key, non-[DatabaseManaged] columns via `col = VALUES(col)`, falling back to all columns when there is nothing else to update.
    private static void EmitUpsert(SourceBuilder builder, MySqlUpsertModel model)
    {
        if (!model.HasEntityType || (model.EntityParamName is null))
        {
            return;
        }

        var columns = model.Columns.Where(static x => !x.Flags.IsDatabaseManaged()).ToList();
        var updates = model.Columns.Where(static x => !x.Flags.IsKey() && !x.Flags.IsDatabaseManaged()).ToList();
        if (updates.Count == 0)
        {
            updates = columns;
        }

        var columnSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
        var valueSql = String.Join(", ", columns.Select(x => model.BindMarker + x.PropertyName));
        var updateSql = String.Join(", ", updates.Select(x => $"{Quote(x.ColumnName)} = VALUES({Quote(x.ColumnName)})"));
        SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({columnSql}) VALUES ({valueSql}) ON DUPLICATE KEY UPDATE {updateSql}");

        foreach (var column in columns)
        {
            SqlEmit.EmitColumnParameter(builder, model.BindMarker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
    }

    // UPDATE を組み立てる。SET=非キー・非 [DatabaseManaged] 列、WHERE=[Key] 列。エンティティが無ければ "UPDATE T SET " のみ。
    // Build an UPDATE. SET = non-key, non-[DatabaseManaged]; WHERE = [Key] columns. Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, MySqlUpdateModel model)
    {
        if (!model.HasEntityType || (model.EntityParamName is null))
        {
            SqlEmit.EmitCommandText(builder, "UPDATE " + Quote(model.TableName) + " SET ");
            return;
        }

        var columns = model.Columns;
        var settable = columns.Where(static x => !x.Flags.IsKey() && !x.Flags.IsDatabaseManaged()).ToList();
        var keys = columns.Where(static x => x.Flags.IsKey()).ToList();

        var sql = new StringBuilder();
        sql.Append("UPDATE ").Append(Quote(model.TableName)).Append(" SET ");
        for (var i = 0; i < settable.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(Quote(settable[i].ColumnName)).Append(" = ").Append(model.BindMarker).Append(settable[i].PropertyName);
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
                sql.Append(Quote(keys[i].ColumnName)).Append(" = ").Append(model.BindMarker).Append("k_").Append(keys[i].PropertyName);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var column in settable)
        {
            SqlEmit.EmitColumnParameter(builder, model.BindMarker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
        foreach (var column in keys)
        {
            SqlEmit.EmitColumnParameter(builder, model.BindMarker + "k_" + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
    }

    // DELETE を組み立てる。WHERE 句はバインドパラメータ（先頭から [Key] 列に対応付け）。
    // Build a DELETE: the WHERE clause uses the bind parameters (mapped to the key columns in order).
    private static void EmitDelete(SourceBuilder builder, MySqlDeleteModel model)
    {
        var keyColumns = model.Columns.Where(static x => x.Flags.IsKey()).ToList();
        var bindParams = SqlEmit.BindParams(model.ValueParams);

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
                sql.Append(Quote(column)).Append(" = ").Append(model.BindMarker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
        }
    }

    // SELECT（全件）。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があれば LIMIT/OFFSET を付ける。
    // Build a SELECT (all rows): SELECT * without an entity; otherwise list columns and append LIMIT/OFFSET when [Limit]/[Offset] are present.
    private static void EmitSelect(SourceBuilder builder, MySqlSelectModel model)
    {
        if (!model.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Quote(model.TableName));
            return;
        }

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(String.Join(", ", model.Columns.Select(x => Quote(x.ColumnName)))).Append(" FROM ").Append(Quote(model.TableName));

        var valueParams = model.ValueParams;
        var limitParam = valueParams.FirstOrDefault(static x => x.Flags.IsLimit());
        var offsetParam = valueParams.FirstOrDefault(static x => x.Flags.IsOffset());
        if ((limitParam is not null) || (offsetParam is not null))
        {
            AppendPaging(
                sql,
                limitParam is null ? null : model.BindMarker + limitParam.Name,
                offsetParam is null ? null : model.BindMarker + offsetParam.Name);
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        if (offsetParam is not null)
        {
            SqlEmit.EmitValueParamBinding(builder, offsetParam, model.BindMarker);
        }
        if (limitParam is not null)
        {
            SqlEmit.EmitValueParamBinding(builder, limitParam, model.BindMarker);
        }
    }

    // SELECT（単一行）。WHERE 句は [Key] 列に対応するバインドパラメータ。
    // Build a SELECT (single row): the WHERE clause uses bind parameters mapped to the [Key] columns.
    private static void EmitSelectSingle(SourceBuilder builder, MySqlSelectSingleModel model)
    {
        if (!model.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Quote(model.TableName));
            return;
        }

        var keyColumns = model.Columns.Where(static x => x.Flags.IsKey()).ToList();
        var bindParams = SqlEmit.BindParams(model.ValueParams);

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
                sql.Append(Quote(column)).Append(" = ").Append(model.BindMarker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
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
