namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using System.Text;

using Smart.Data.Accessor.Builders.SqlServer.Generator.Models;
using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SQL Server Builder の emit：種別毎の SQL 組み立て（bracket クォート、OFFSET/FETCH ページング、MERGE/OUTPUT）。プリミティブは共有 SqlEmit。
// Emit for the SQL Server builder: per-kind SQL assembly (bracket quoting, OFFSET/FETCH paging, MERGE/OUTPUT). Primitives via the shared SqlEmit.
internal static class SqlServerSourceBuilder
{
    public static void EmitMethod(SourceBuilder builder, SqlServerMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method.MethodName, method.ValueParams);

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

    // INSERT を組み立てる（OUTPUT 句対応）。エンティティモードはエンティティ列（[DatabaseManaged] 除外）、パラメータモードはバインドパラメータ。
    // Build an INSERT (with OUTPUT). Entity mode uses entity columns (excluding [DatabaseManaged]); parameter mode uses the bind parameters.
    private static void EmitInsert(SourceBuilder builder, SqlServerInsertModel model)
    {
        if (model.EntityParamName is not null)
        {
            var columns = model.Columns.Where(static x => !x.Flags.IsDatabaseManaged()).ToList();
            var columnSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
            var valueSql = String.Join(", ", columns.Select(x => model.BindMarker + x.PropertyName));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({columnSql}){OutputClause(model.OutputColumns, "INSERTED")} VALUES ({valueSql})");
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
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({columnSql}){OutputClause(model.OutputColumns, "INSERTED")} VALUES ({valueSql})");
            foreach (var parameter in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
            }
        }
    }

    // UPDATE を組み立てる（OUTPUT 句対応）。SET=非キー・非 [DatabaseManaged] 列、WHERE=[Key] 列。エンティティが無ければ "UPDATE T SET " のみ。
    // Build an UPDATE (with OUTPUT). SET = non-key, non-[DatabaseManaged]; WHERE = [Key] columns. Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, SqlServerUpdateModel model)
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

    // DELETE を組み立てる（OUTPUT 句対応）。WHERE=バインドパラメータ（[Key] 列に対応付け）。
    // Build a DELETE (with OUTPUT). WHERE = bind parameters (mapped to the key columns in order).
    private static void EmitDelete(SourceBuilder builder, SqlServerDeleteModel model)
    {
        var keyColumns = model.Columns.Where(static x => x.Flags.IsKey()).ToList();
        var bindParams = SqlEmit.BindParams(model.ValueParams);

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
                sql.Append(Quote(column)).Append(" = ").Append(model.BindMarker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
        }
    }

    // SELECT（全件）。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があれば OFFSET/FETCH を付ける。
    // Build a SELECT (all rows): SELECT * without an entity; otherwise list columns and append OFFSET/FETCH when [Limit]/[Offset] are present.
    private static void EmitSelect(SourceBuilder builder, SqlServerSelectModel model)
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
    private static void EmitSelectSingle(SourceBuilder builder, SqlServerSelectSingleModel model)
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

    // MERGE による UPSERT。USING で束縛値の仮想行 S を作り [Key] で突合、WHEN MATCHED で非キー・非 [DatabaseManaged] 列を更新、WHEN NOT MATCHED で INSERT。
    // Build a MERGE upsert: a source row S from the bound values, matched on [Key]; WHEN MATCHED updates the non-key, non-[DatabaseManaged] columns and WHEN NOT MATCHED inserts.
    private static void EmitMerge(SourceBuilder builder, SqlServerMergeModel model)
    {
        if (!model.HasEntityType || (model.EntityParamName is null))
        {
            return;
        }

        var columns = model.Columns.Where(static x => !x.Flags.IsDatabaseManaged()).ToList();
        var keys = model.Columns.Where(static x => x.Flags.IsKey()).ToList();
        var updates = model.Columns.Where(static x => !x.Flags.IsKey() && !x.Flags.IsDatabaseManaged()).ToList();

        var sql = new StringBuilder();
        sql.Append("MERGE INTO ").Append(Quote(model.TableName)).Append(" AS T USING (SELECT ");
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(model.BindMarker).Append(columns[i].PropertyName).Append(" AS ").Append(Quote(columns[i].ColumnName));
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
            SqlEmit.EmitColumnParameter(builder, model.BindMarker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
        }
    }

    // OUTPUT 句を組み立てる。outputColumns（カンマ区切りの列名）を pseudoTable（INSERTED / DELETED）の列として返す。未指定なら空文字。
    // Build the OUTPUT clause: render outputColumns (comma-separated) as columns of pseudoTable (INSERTED / DELETED). Empty when absent.
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
