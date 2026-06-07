namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.Postgres.Generator.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// PostgreSQL Builder の emit：種別毎の SQL 組み立て（二重引用符、LIMIT/OFFSET、ON CONFLICT、RETURNING）。プリミティブは共有 SqlEmit。
// Emit for the PostgreSQL builder: per-kind SQL assembly (double-quote quoting, LIMIT/OFFSET, ON CONFLICT, RETURNING). Primitives via the shared SqlEmit.
internal static class PostgresSourceBuilder
{
    // 1 メソッド分のヘルパーを出力する。シグネチャと cmd 取得・スコープ開閉は共有の SqlEmit、種別毎の本体はこのプロバイダーが持つ。
    // Emit one method's helper. The signature, cmd acquisition and scope open/close come from the shared SqlEmit; the
    // per-kind body is owned by this provider.
    public static void EmitMethod(SourceBuilder builder, BuilderMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method);

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

    // INSERT を組み立てる。エンティティモード（typeof(T) 指定）はエンティティ列（[DatabaseManaged] は除外）を、
    // パラメータモード（Table 指定）はバインドパラメータを列・値にする。RETURNING 句があれば末尾に付加。
    // Build an INSERT. Entity mode (typeof(T)) uses the entity columns (excluding [DatabaseManaged]); parameter mode
    // (Table = "...") uses the bind parameters as columns/values. Appends the RETURNING clause when present.
    private static void EmitInsert(SourceBuilder builder, PostgresInsertModel model)
    {
        if (model.EntityParamName is not null)
        {
            // エンティティモード：列はエンティティのプロパティから（DB が値を管理する [DatabaseManaged] 列は除外）。
            // Entity mode: columns from entity properties (excluding [DatabaseManaged], which the DB fills in).
            var columns = model.Columns.Where(static x => !x.IsDatabaseManaged).ToList();
            var colSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
            var valSql = String.Join(", ", columns.Select(x => SqlEmit.Marker + x.PropertyName));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({colSql}) VALUES ({valSql}){ReturningClause(model.ReturningColumns)}");
            foreach (var column in columns)
            {
                SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
            }
        }
        else
        {
            // パラメータモード：列・値はバインドパラメータから組む。
            // Parameter mode: columns / values come from the bind parameters.
            var bindParams = SqlEmit.BindParams(model);
            var colSql = String.Join(", ", bindParams.Select(x => Quote(x.ColumnName)));
            var valSql = String.Join(", ", bindParams.Select(x => SqlEmit.Marker + x.Name));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({colSql}) VALUES ({valSql}){ReturningClause(model.ReturningColumns)}");
            foreach (var parameter in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, parameter);
            }
        }
    }

    // UPDATE を組み立てる。SET 句は非キーかつ非 [DatabaseManaged] 列、WHERE 句は [Key] 列（@k_ 接頭辞のパラメータ）。RETURNING 句があれば末尾に付加。
    // エンティティが無い場合は "UPDATE T SET " だけを出力する。
    // Build an UPDATE: the SET clause uses non-key, non-[DatabaseManaged] columns; the WHERE clause uses [Key] columns
    // (parameters prefixed @k_); the RETURNING clause (if any) is appended. Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, PostgresUpdateModel model)
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

        sql.Append(ReturningClause(model.ReturningColumns));

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

    // DELETE を組み立てる。WHERE 句はバインドパラメータ（先頭から [Key] 列に対応付け）。RETURNING 句があれば末尾に付加。
    // Build a DELETE: the WHERE clause uses the bind parameters (mapped to the key columns in order); the RETURNING clause (if any) is appended.
    private static void EmitDelete(SourceBuilder builder, PostgresDeleteModel model)
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

        sql.Append(ReturningClause(model.ReturningColumns));

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter);
        }
    }

    // SELECT（全件）を組み立てる。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があればプロバイダのページング句を付ける。
    // Build a SELECT (all rows): SELECT * when there is no entity; otherwise list the columns and, if [Limit]/[Offset]
    // are present, append the provider's paging clause.
    private static void EmitSelect(SourceBuilder builder, PostgresSelectModel model)
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
    private static void EmitSelectSingle(SourceBuilder builder, PostgresSelectSingleModel model)
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

    // INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col を組み立てる。INSERT 列は非 [DatabaseManaged]、突合は [Key]、
    // 更新は非キー・非 [DatabaseManaged] 列。更新対象が無ければ DO NOTHING。パラメータ束縛は INSERT エンティティモードと同じ。
    // Build INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col. INSERT columns are non-[DatabaseManaged]; the
    // conflict target is the [Key] columns; updates assign the non-key, non-[DatabaseManaged] columns. DO NOTHING when
    // there is nothing to update. Parameter binding is the same as INSERT entity mode.
    private static void EmitUpsert(SourceBuilder builder, PostgresUpsertModel model)
    {
        if (!model.HasEntityType || (model.EntityParamName is null))
        {
            // エンティティ実体が無く列を解決できない場合は何も組み立てない（診断は transform 側で報告済み）。
            // Without an entity instance the column list is unresolved; emit nothing (the diagnostic is raised in the transform).
            return;
        }

        var columns = model.Columns.Where(static x => !x.IsDatabaseManaged).ToList();
        var keys = model.Columns.Where(static x => x.IsKey).ToList();
        var updates = model.Columns.Where(static x => !x.IsKey && !x.IsDatabaseManaged).ToList();

        var colSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
        var valSql = String.Join(", ", columns.Select(x => SqlEmit.Marker + x.PropertyName));
        var conflictSql = String.Join(", ", keys.Select(x => Quote(x.ColumnName)));

        var sql = new StringBuilder();
        sql.Append("INSERT INTO ").Append(Quote(model.TableName)).Append(" (").Append(colSql).Append(") VALUES (").Append(valSql).Append(") ON CONFLICT (").Append(conflictSql).Append(')');
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
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
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
