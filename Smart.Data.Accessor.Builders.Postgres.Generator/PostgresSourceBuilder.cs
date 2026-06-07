namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.Postgres.Generator.Models;
using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// PostgreSQL Builder の emit：種別毎の SQL 組み立て（二重引用符、LIMIT/OFFSET、ON CONFLICT、RETURNING）。プリミティブは共有 SqlEmit。
// Emit for the PostgreSQL builder: per-kind SQL assembly (double-quote quoting, LIMIT/OFFSET, ON CONFLICT, RETURNING). Primitives via the shared SqlEmit.
internal static class PostgresSourceBuilder
{
    public static void EmitMethod(SourceBuilder builder, PostgresMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method.MethodName, method.ValueParams);

        switch (method)
        {
            case PostgresInsertModel model:
                EmitInsert(builder, model);
                break;
            case PostgresUpdateModel model:
                EmitUpdate(builder, model);
                break;
            case PostgresDeleteModel model:
                EmitDelete(builder, model);
                break;
            case PostgresCountModel model:
                SqlEmit.EmitCommandText(builder, "SELECT COUNT(*) FROM " + Quote(model.TableName));
                break;
            case PostgresTruncateModel model:
                SqlEmit.EmitCommandText(builder, "TRUNCATE TABLE " + Quote(model.TableName));
                break;
            case PostgresSelectModel model:
                EmitSelect(builder, model);
                break;
            case PostgresSelectSingleModel model:
                EmitSelectSingle(builder, model);
                break;
            case PostgresUpsertModel model:
                EmitUpsert(builder, model);
                break;
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT を組み立てる（RETURNING 句対応）。エンティティモードはエンティティ列（[DatabaseManaged] 除外）、パラメータモードはバインドパラメータ。
    // Build an INSERT (with RETURNING). Entity mode uses entity columns (excluding [DatabaseManaged]); parameter mode uses the bind parameters.
    private static void EmitInsert(SourceBuilder builder, PostgresInsertModel model)
    {
        if (model.EntityParamName is not null)
        {
            var columns = model.Columns.Where(static x => !x.Flags.IsDatabaseManaged()).ToList();
            var columnSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
            var valueSql = String.Join(", ", columns.Select(x => model.BindMarker + x.PropertyName));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({columnSql}) VALUES ({valueSql}){ReturningClause(model.ReturningColumns)}");
            foreach (var column in columns)
            {
                SqlEmit.EmitColumnParameter(builder, model.BindMarker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
            }
        }
        else
        {
            var bindParams = SqlEmit.BindParams(model.ValueParams);
            var columnSql = String.Join(", ", bindParams.Select(x => Quote(x.ColumnName)));
            var valueSql = String.Join(", ", bindParams.Select(x => model.BindMarker + x.Name));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({columnSql}) VALUES ({valueSql}){ReturningClause(model.ReturningColumns)}");
            foreach (var parameter in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
            }
        }
    }

    // UPDATE を組み立てる（RETURNING 句対応）。SET=非キー・非 [DatabaseManaged] 列、WHERE=[Key] 列。エンティティが無ければ "UPDATE T SET " のみ。
    // Build an UPDATE (with RETURNING). SET = non-key, non-[DatabaseManaged]; WHERE = [Key] columns. Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, PostgresUpdateModel model)
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

        sql.Append(ReturningClause(model.ReturningColumns));

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

    // DELETE を組み立てる（RETURNING 句対応）。WHERE=バインドパラメータ（[Key] 列に対応付け）。
    // Build a DELETE (with RETURNING). WHERE = bind parameters (mapped to the key columns in order).
    private static void EmitDelete(SourceBuilder builder, PostgresDeleteModel model)
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

        sql.Append(ReturningClause(model.ReturningColumns));

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
        }
    }

    // SELECT（全件）。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があれば LIMIT/OFFSET を付ける。
    // Build a SELECT (all rows): SELECT * without an entity; otherwise list columns and append LIMIT/OFFSET when [Limit]/[Offset] are present.
    private static void EmitSelect(SourceBuilder builder, PostgresSelectModel model)
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
    private static void EmitSelectSingle(SourceBuilder builder, PostgresSelectSingleModel model)
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

    // INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col。INSERT 列は非 [DatabaseManaged]、突合は [Key]、更新は非キー・非 [DatabaseManaged] 列。更新対象が無ければ DO NOTHING。
    // Build INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col. INSERT columns are non-[DatabaseManaged]; conflict target = [Key]; updates assign the non-key, non-[DatabaseManaged] columns. DO NOTHING when nothing to update.
    private static void EmitUpsert(SourceBuilder builder, PostgresUpsertModel model)
    {
        if (!model.HasEntityType || (model.EntityParamName is null))
        {
            return;
        }

        var columns = model.Columns.Where(static x => !x.Flags.IsDatabaseManaged()).ToList();
        var keys = model.Columns.Where(static x => x.Flags.IsKey()).ToList();
        var updates = model.Columns.Where(static x => !x.Flags.IsKey() && !x.Flags.IsDatabaseManaged()).ToList();

        var columnSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
        var valueSql = String.Join(", ", columns.Select(x => model.BindMarker + x.PropertyName));
        var conflictSql = String.Join(", ", keys.Select(x => Quote(x.ColumnName)));

        var sql = new StringBuilder();
        sql.Append("INSERT INTO ").Append(Quote(model.TableName)).Append(" (").Append(columnSql).Append(") VALUES (").Append(valueSql).Append(") ON CONFLICT (").Append(conflictSql).Append(')');
        if (updates.Count > 0)
        {
            sql.Append(" DO UPDATE SET ");
            for (var i = 0; i < updates.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }
                sql.Append(Quote(updates[i].ColumnName)).Append(" = EXCLUDED.").Append(Quote(updates[i].ColumnName));
            }
        }
        else
        {
            sql.Append(" DO NOTHING");
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var column in columns)
        {
            SqlEmit.EmitColumnParameter(builder, model.BindMarker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
    }

    // RETURNING 句を組み立てる。returningColumns（カンマ区切りの列名）を返す列として並べる。未指定なら空文字。
    // Build the RETURNING clause from returningColumns (comma-separated column names). Empty when absent.
    private static string ReturningClause(string? returningColumns)
    {
        if (returningColumns is null)
        {
            return string.Empty;
        }

        var parts = returningColumns.Split(',').Select(static x => x.Trim()).Where(static x => x.Length > 0).ToList();
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return " RETURNING " + String.Join(", ", parts.Select(Quote));
    }

    // 識別子を二重引用符でクォートする（" は "" にエスケープ）。
    // Quote an identifier with double quotes (escaping " as "").
    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"") + "\"";

    // LIMIT/OFFSET ページングを付加する（両者は独立）。
    // Append LIMIT/OFFSET paging (the two are independent).
    private static void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker)
    {
        if (limitMarker is not null)
        {
            sql.Append(" LIMIT ").Append(limitMarker);
        }
        if (offsetMarker is not null)
        {
            sql.Append(" OFFSET ").Append(offsetMarker);
        }
    }
}
