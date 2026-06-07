namespace Smart.Data.Accessor.Generator.Builders;

using System.Text;

using Smart.Data.Accessor.Generator.Builders.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// 標準（既定）Builder の emit：種別毎の SQL 組み立て。共有プリミティブは SqlEmit、識別子クォート・ページングは本クラスの Quote/AppendPaging。
// Emit for the standard (default) builder: per-kind SQL assembly. Shared primitives via SqlEmit; identifier quoting /
// paging via this class's own Quote / AppendPaging.
internal static class StandardSourceBuilder
{
    // 1 メソッド分のヘルパーを出力する。シグネチャと cmd 取得・スコープ開閉は共有の SqlEmit、種別毎の本体はこのプロバイダーが持つ。
    // Emit one method's helper. The signature, cmd acquisition and scope open/close come from the shared SqlEmit; the
    // per-kind body is owned by this provider.
    public static void EmitMethod(SourceBuilder builder, BuilderMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method);

        switch (method)
        {
            case InsertModel model:
                EmitInsert(builder, model);
                break;
            case UpdateModel model:
                EmitUpdate(builder, model);
                break;
            case DeleteModel model:
                EmitDelete(builder, model);
                break;
            case CountModel model:
                SqlEmit.EmitCommandText(builder, "SELECT COUNT(*) FROM " + Quote(model.TableName));
                break;
            case TruncateModel model:
                SqlEmit.EmitCommandText(builder, "TRUNCATE TABLE " + Quote(model.TableName));
                break;
            case SelectModel model:
                EmitSelect(builder, model);
                break;
            case SelectSingleModel model:
                EmitSelectSingle(builder, model);
                break;
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT を組み立てる。エンティティモード（typeof(T) 指定）はエンティティ列（[DatabaseManaged] は除外）を、
    // パラメータモード（Table 指定）はバインドパラメータを列・値にする。
    // Build an INSERT. Entity mode (typeof(T)) uses the entity columns (excluding [DatabaseManaged]); parameter mode
    // (Table = "...") uses the bind parameters as columns/values.
    private static void EmitInsert(SourceBuilder builder, InsertModel model)
    {
        if (model.EntityParamName is not null)
        {
            // エンティティモード：列はエンティティのプロパティから（DB が値を管理する [DatabaseManaged] 列は除外）。
            // Entity mode: columns from entity properties (excluding [DatabaseManaged], which the DB fills in).
            var columns = model.Columns.Where(static x => !x.IsDatabaseManaged).ToList();
            var columnSql = String.Join(", ", columns.Select(x => Quote(x.ColumnName)));
            var valueSql = String.Join(", ", columns.Select(x => model.BindMarker + x.PropertyName));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({columnSql}) VALUES ({valueSql})");
            foreach (var column in columns)
            {
                SqlEmit.EmitColumnParameter(builder, model.BindMarker + column.PropertyName, $"{model.EntityParamName}.{column.PropertyName}", column);
            }
        }
        else
        {
            // パラメータモード：列・値はバインドパラメータから組む。
            // Parameter mode: columns / values come from the bind parameters.
            var bindParams = SqlEmit.BindParams(model);
            var columnSql = String.Join(", ", bindParams.Select(x => Quote(x.ColumnName)));
            var valueSql = String.Join(", ", bindParams.Select(x => model.BindMarker + x.Name));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Quote(model.TableName)} ({columnSql}) VALUES ({valueSql})");
            foreach (var parameter in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
            }
        }
    }

    // UPDATE を組み立てる。SET 句は非キーかつ非 [DatabaseManaged] 列、WHERE 句は [Key] 列（@k_ 接頭辞のパラメータ）。
    // エンティティが無い場合は "UPDATE T SET " だけを出力する。
    // Build an UPDATE: the SET clause uses non-key, non-[DatabaseManaged] columns; the WHERE clause uses [Key] columns
    // (parameters prefixed @k_). Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, UpdateModel model)
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
    private static void EmitDelete(SourceBuilder builder, DeleteModel model)
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
                sql.Append(Quote(column)).Append(" = ").Append(model.BindMarker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
        }
    }

    // SELECT（全件）を組み立てる。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があればプロバイダのページング句を付ける。
    // Build a SELECT (all rows): SELECT * when there is no entity; otherwise list the columns and, if [Limit]/[Offset]
    // are present, append the provider's paging clause.
    private static void EmitSelect(SourceBuilder builder, SelectModel model)
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

    // SELECT（単一行）を組み立てる。WHERE 句は [Key] 列に対応するバインドパラメータ。
    // Build a SELECT (single row): the WHERE clause uses bind parameters mapped to the [Key] columns.
    private static void EmitSelectSingle(SourceBuilder builder, SelectSingleModel model)
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
                sql.Append(Quote(column)).Append(" = ").Append(model.BindMarker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var parameter in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, parameter, model.BindMarker);
        }
    }

    // 識別子を二重引用符でクォートする（" は "" にエスケープ。SQLite / MySQL / PostgreSQL で有効）。
    // Quote an identifier with double quotes (escaping " as ""; valid for SQLite / MySQL / PostgreSQL).
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
