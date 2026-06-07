namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.SqlServer.Generator.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// SQL Server Builder の emit：種別毎の SQL 組み立て（bracket クォート、OFFSET/FETCH ページング、MERGE/OUTPUT）。プリミティブは共有 SqlEmit。
// Emit for the SQL Server builder: per-kind SQL assembly (bracket quoting, OFFSET/FETCH paging, MERGE/OUTPUT). Primitives via the shared SqlEmit.
internal static class SqlServerSourceBuilder
{
    // 1 メソッド分のヘルパーを出力する。シグネチャと cmd 取得・スコープ開閉は共有の SqlEmit、種別毎の本体はこのプロバイダーが持つ。
    // Emit one method's helper. The signature, cmd acquisition and scope open/close come from the shared SqlEmit; the
    // per-kind body is owned by this provider.
    public static void EmitMethod(SourceBuilder builder, BuilderMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method);

        switch (method)
        {
            case SqlServerInsertModel model:
                EmitInsert(builder, model);
                break;
            case SqlServerUpdateModel model:
                EmitUpdate(builder, model);
                break;
            case SqlServerDeleteModel model:
                EmitDelete(builder, model);
                break;
            case SqlServerCountModel model:
                SqlEmit.EmitCommandText(builder, "SELECT COUNT(*) FROM " + Quote(model.TableName));
                break;
            case SqlServerTruncateModel model:
                SqlEmit.EmitCommandText(builder, "TRUNCATE TABLE " + Quote(model.TableName));
                break;
            case SqlServerSelectModel model:
                EmitSelect(builder, model);
                break;
            case SqlServerSelectSingleModel model:
                EmitSelectSingle(builder, model);
                break;
            case SqlServerMergeModel model:
                EmitMerge(builder, model);
                break;
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT を組み立てる。エンティティモード（typeof(T) 指定）はエンティティ列（[DatabaseManaged] は除外）を、
    // パラメータモード（Table 指定）はバインドパラメータを列・値にする。OUTPUT 句があれば付加。
    // Build an INSERT. Entity mode (typeof(T)) uses the entity columns (excluding [DatabaseManaged]); parameter mode
    // (Table = "...") uses the bind parameters as columns/values. Appends the OUTPUT clause when present.
    private static void EmitInsert(SourceBuilder builder, SqlServerInsertModel model)
    {
        if (model.EntityParamName is not null)
        {
            // エンティティモード：列はエンティティのプロパティから（DB が値を管理する [DatabaseManaged] 列は除外）。
            // Entity mode: columns from entity properties (excluding [DatabaseManaged], which the DB fills in).
            var columns = model.Columns.Where(static x => !x.IsDatabaseManaged).ToList();
            var colSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
            var valSql = String.Join(", ", columns.Select(x => SqlEmit.Marker + x.PropertyName));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({colSql}){OutputClause(model.OutputColumns, "INSERTED")} VALUES ({valSql})");
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
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({colSql}){OutputClause(model.OutputColumns, "INSERTED")} VALUES ({valSql})");
            foreach (var parameter in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, parameter);
            }
        }
    }

    // UPDATE を組み立てる。SET 句は非キーかつ非 [DatabaseManaged] 列、WHERE 句は [Key] 列（@k_ 接頭辞のパラメータ）。OUTPUT 句があれば SET の後に付加。
    // エンティティが無い場合は "UPDATE T SET " だけを出力する。
    // Build an UPDATE: the SET clause uses non-key, non-[DatabaseManaged] columns; the WHERE clause uses [Key] columns
    // (parameters prefixed @k_); the OUTPUT clause (if any) follows SET. Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, SqlServerUpdateModel model)
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
        sql.Append(OutputClause(model.OutputColumns, "INSERTED"));
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

    // DELETE を組み立てる。WHERE 句はバインドパラメータ（先頭から [Key] 列に対応付け）。OUTPUT 句があればテーブル名の後に付加。
    // Build a DELETE: the WHERE clause uses the bind parameters (mapped to the key columns in order); the OUTPUT clause (if any) follows the table.
    private static void EmitDelete(SourceBuilder builder, SqlServerDeleteModel model)
    {
        var keyColumns = model.Columns.Where(static x => x.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(model);

        var sql = new StringBuilder();
        sql.Append("DELETE FROM ").Append(Quote(model.TableName));
        sql.Append(OutputClause(model.OutputColumns, "DELETED"));
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
    private static void EmitSelect(SourceBuilder builder, SqlServerSelectModel model)
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
    private static void EmitSelectSingle(SourceBuilder builder, SqlServerSelectSingleModel model)
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

    // MERGE による UPSERT を組み立てる。USING で束縛値の仮想行 S を作り、[Key] で突合、WHEN MATCHED で非キー・非 [DatabaseManaged] 列を
    // 更新、WHEN NOT MATCHED で INSERT。更新列が無ければ WHEN MATCHED を省略。パラメータ束縛は INSERT エンティティモードと同じ。
    // Build a MERGE upsert: a source row S from the bound values, matched on [Key]; WHEN MATCHED updates the non-key,
    // non-[DatabaseManaged] columns and WHEN NOT MATCHED inserts. The WHEN MATCHED clause is omitted when nothing is
    // updatable. Parameter binding is the same as INSERT entity mode.
    private static void EmitMerge(SourceBuilder builder, SqlServerMergeModel model)
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

        var sql = new StringBuilder();
        sql.Append("MERGE INTO ").Append(Quote(model.TableName)).Append(" AS T USING (SELECT ");
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(SqlEmit.Marker).Append(columns[i].PropertyName).Append(" AS ").Append(Quote(columns[i].ColumnName));
        }
        sql.Append(") AS S ON (");
        for (var i = 0; i < keys.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(" AND ");
            }
            sql.Append("T.").Append(Quote(keys[i].ColumnName)).Append(" = S.").Append(Quote(keys[i].ColumnName));
        }
        sql.Append(')');
        if (updates.Count > 0)
        {
            sql.Append(" WHEN MATCHED THEN UPDATE SET ");
            for (var i = 0; i < updates.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }
                sql.Append("T.").Append(Quote(updates[i].ColumnName)).Append(" = S.").Append(Quote(updates[i].ColumnName));
            }
        }
        sql.Append(" WHEN NOT MATCHED THEN INSERT (");
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(Quote(columns[i].ColumnName));
        }
        sql.Append(") VALUES (");
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append("S.").Append(Quote(columns[i].ColumnName));
        }
        sql.Append(");");

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var column in columns)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
    }

    // OUTPUT 句を組み立てる。outputColumns（カンマ区切りの列名）を pseudoTable（INSERTED / DELETED）の列として返す。未指定なら空文字。
    // Build the OUTPUT clause: render outputColumns (comma-separated column names) as columns of pseudoTable (INSERTED / DELETED). Empty when absent.
    private static string OutputClause(string? outputColumns, string pseudoTable)
    {
        if (outputColumns is null)
        {
            return string.Empty;
        }

        var parts = outputColumns.Split(',').Select(static x => x.Trim()).Where(static x => x.Length > 0).ToList();
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return " OUTPUT " + String.Join(", ", parts.Select(x => pseudoTable + "." + Quote(x)));
    }

    // 識別子を角括弧でクォートする（] は ]] にエスケープ）。
    // Quote an identifier with brackets (escaping ] as ]]).
    private static string Quote(string identifier) => "[" + identifier.Replace("]", "]]") + "]";

    // OFFSET/FETCH ページングを付加する。OFFSET/FETCH は ORDER BY を要するため、未指定時は正規の no-op 並びを補う。
    // Append OFFSET/FETCH paging. OFFSET/FETCH requires an ORDER BY, so supply the canonical no-op ordering when none is given.
    private static void AppendPaging(StringBuilder sql, string? limitMarker, string? offsetMarker)
    {
        sql.Append(" ORDER BY (SELECT NULL) OFFSET ").Append(offsetMarker ?? "0").Append(" ROWS");
        if (limitMarker is not null)
        {
            sql.Append(" FETCH NEXT ").Append(limitMarker).Append(" ROWS ONLY");
        }
    }
}
