namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.Generator.Engine;
using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// PostgreSQL QueryBuilder ジェネレータ。[PgInsert]/…/[PgTruncate] が付いたメソッドに {Method}__QueryBuilder ヘルパーを
// 生成する（二重引用符クォート、LIMIT/OFFSET ページング）。[DataAccessor] で登録し、走査・出力スキャフォールド・SQL プリミティブは共有
// メカニクス（BuilderClassScanner / BuilderOutput / SqlEmit / MethodResolver）に委譲しつつ、種別の判定（PostgresKind）・Model 構築・
// 各種別の SQL 組み立て（Execute）はこのプロバイダーが自前で持つ。
// The PostgreSQL QueryBuilder generator: emits the {Method}__QueryBuilder helper for methods carrying the
// [PgInsert]/…/[PgTruncate] attributes (double-quote quoting, LIMIT/OFFSET paging). Registers on
// [DataAccessor] and delegates scanning / output scaffolding / SQL primitives to the shared mechanics
// (BuilderClassScanner / BuilderOutput / SqlEmit / MethodResolver), while owning its own kind dispatch (PostgresKind),
// model construction, and per-kind SQL assembly (Execute).
[Generator]
public sealed class PostgresQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.Postgres.Pg";

    private static readonly (string Attribute, PostgresKind Kind)[] Targets =
    [
        (Ns + "InsertAttribute", PostgresKind.Insert),
        (Ns + "UpdateAttribute", PostgresKind.Update),
        (Ns + "DeleteAttribute", PostgresKind.Delete),
        (Ns + "CountAttribute", PostgresKind.Count),
        (Ns + "SelectAttribute", PostgresKind.Select),
        (Ns + "SelectSingleAttribute", PostgresKind.SelectSingle),
        (Ns + "TruncateAttribute", PostgresKind.Truncate),
        (Ns + "UpsertAttribute", PostgresKind.Upsert),
    ];

    private static readonly SqlDialect Dialect = new PostgresDialect();

    // 入口：[DataAccessor] クラスを FAWMN で拾い、共有スキャナに自前の Targets と BuildMethod（transform）を渡して等価 Model を得て、
    // 共有出力段に自前の EmitMethod（execute）を渡して生成する。
    // Entry point: pick up [DataAccessor] classes via FAWMN, pass this provider's Targets + BuildMethod (transform) to
    // the shared scanner to get the equatable model, then drive the shared output stage with this provider's EmitMethod (execute).
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var provider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                BuilderClassScanner.DataAccessorAttributeName,
                static (node, _) => node is ClassDeclarationSyntax,
                static (ctx, ct) => BuilderClassScanner.Scan(ctx, Targets, BuildMethod, ct))
            .WithTrackingName(BuilderClassScanner.TrackingName);

        context.RegisterSourceOutput(provider, static (spc, model) => BuilderOutput.Emit(spc, model, EmitMethod, ".Postgres"));
    }

    // === Transform（種別判定と Model 構築。共通解決は MethodResolver に委譲） ===
    // === Transform (kind dispatch + model construction; common resolution is delegated to MethodResolver) ===

    // 1 つの QueryBuilder メソッドを Model 化する。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、
    // ここでは PostgresKind 別の Model 生成と診断（キー欠如など）だけを行う。解決できない場合は null を返す。
    // Build the per-kind model for one QueryBuilder method. Common resolution (table / value params / columns) is
    // delegated to MethodResolver; only the PostgresKind-specific model construction and diagnostics happen here.
    // Returns null when the method cannot be resolved.
    private static BuilderMethodModel? BuildMethod(MethodBuildContext<PostgresKind> c)
    {
        var r = MethodResolver.Resolve(c.Container, c.Method, c.Attr, c.TypeMaps, c.Profile, c.Diagnostics, c.Location);
        if (r is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle はキー（[Key]）が無いと WHERE を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle need a key ([Key]) to build the WHERE clause, so they
        // raise a diagnostic when none is present.
        switch (c.Kind)
        {
            case PostgresKind.Insert:
                return new PostgresInsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, ReadReturningColumns(c.Attr));

            case PostgresKind.Update:
                if (!r.HasEntityType || (r.EntityParamName is null))
                {
                    // SDA1004: 列リストを解決できない（エンティティ実体が無い）。
                    // SDA1004: cannot resolve the column list (no entity instance).
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new PostgresUpdateModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, r.HasEntityType, ReadReturningColumns(c.Attr));

            case PostgresKind.Delete:
                if (r.HasEntityType && !r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new PostgresDeleteModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType, ReadReturningColumns(c.Attr));

            case PostgresKind.Count:
                return new PostgresCountModel(r.MethodName, r.TableName, r.ValueParams);

            case PostgresKind.Truncate:
                return new PostgresTruncateModel(r.MethodName, r.TableName, r.ValueParams);

            case PostgresKind.Select:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                return new PostgresSelectModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case PostgresKind.SelectSingle:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new PostgresSelectSingleModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case PostgresKind.Upsert:
                if (!r.HasEntityType || (r.EntityParamName is null))
                {
                    // SDA1004: 列リストを解決できない（エンティティ実体が無い）。
                    // SDA1004: cannot resolve the column list (no entity instance).
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new PostgresUpsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, r.HasEntityType);

            default:
                return null;
        }
    }

    // === Execute（種別毎の SQL 組み立て。プリミティブは SqlEmit、識別子クォート・ページングは Dialect） ===
    // === Execute (per-kind SQL assembly; primitives via SqlEmit, identifier quoting / paging via Dialect) ===

    // 1 メソッド分のヘルパーを出力する。シグネチャと cmd 取得・スコープ開閉は共有の SqlEmit、種別毎の本体はこのプロバイダーが持つ。
    // Emit one method's helper. The signature, cmd acquisition and scope open/close come from the shared SqlEmit; the
    // per-kind body is owned by this provider.
    private static void EmitMethod(SourceBuilder builder, BuilderMethodModel method)
    {
        SqlEmit.OpenMethod(builder, method);

        switch (method)
        {
            case PostgresInsertModel m:
                EmitInsert(builder, m);
                break;
            case PostgresUpdateModel m:
                EmitUpdate(builder, m);
                break;
            case PostgresDeleteModel m:
                EmitDelete(builder, m);
                break;
            case PostgresCountModel m:
                SqlEmit.EmitCommandText(builder, "SELECT COUNT(*) FROM " + Dialect.Quote(m.TableName));
                break;
            case PostgresTruncateModel m:
                SqlEmit.EmitCommandText(builder, "TRUNCATE TABLE " + Dialect.Quote(m.TableName));
                break;
            case PostgresSelectModel m:
                EmitSelect(builder, m);
                break;
            case PostgresSelectSingleModel m:
                EmitSelectSingle(builder, m);
                break;
            case PostgresUpsertModel m:
                EmitUpsert(builder, m);
                break;
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT を組み立てる。エンティティモード（typeof(T) 指定）はエンティティ列（[DatabaseManaged] は除外）を、
    // パラメータモード（Table 指定）はバインドパラメータを列・値にする。
    // Build an INSERT. Entity mode (typeof(T)) uses the entity columns (excluding [DatabaseManaged]); parameter mode
    // (Table = "...") uses the bind parameters as columns/values.
    private static void EmitInsert(SourceBuilder builder, PostgresInsertModel m)
    {
        if (m.EntityParamName is not null)
        {
            // エンティティモード：列はエンティティのプロパティから（DB が値を管理する [DatabaseManaged] 列は除外）。
            // Entity mode: columns from entity properties (excluding [DatabaseManaged], which the DB fills in).
            var cols = m.Columns.Where(static c => !c.IsDatabaseManaged).ToList();
            var colSql = String.Join(", ", cols.Select(c => Dialect.Quote(c.ColumnName)));
            var valSql = String.Join(", ", cols.Select(c => SqlEmit.Marker + c.PropertyName));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Dialect.Quote(m.TableName)} ({colSql}) VALUES ({valSql}){ReturningClause(m.ReturningColumns)}");
            foreach (var c in cols)
            {
                SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + c.PropertyName, $"{m.EntityParamName}.{c.PropertyName}", c);
            }
        }
        else
        {
            // パラメータモード：列・値はバインドパラメータから組む。
            // Parameter mode: columns / values come from the bind parameters.
            var bindParams = SqlEmit.BindParams(m);
            var colSql = String.Join(", ", bindParams.Select(p => Dialect.Quote(p.ColumnName)));
            var valSql = String.Join(", ", bindParams.Select(p => SqlEmit.Marker + p.Name));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Dialect.Quote(m.TableName)} ({colSql}) VALUES ({valSql}){ReturningClause(m.ReturningColumns)}");
            foreach (var p in bindParams)
            {
                SqlEmit.EmitValueParamBinding(builder, p);
            }
        }
    }

    // UPDATE を組み立てる。SET 句は非キーかつ非 [DatabaseManaged] 列、WHERE 句は [Key] 列（@k_ 接頭辞のパラメータ）。
    // エンティティが無い場合は "UPDATE T SET " だけを出力する。
    // Build an UPDATE: the SET clause uses non-key, non-[DatabaseManaged] columns; the WHERE clause uses [Key] columns
    // (parameters prefixed @k_). Without an entity it emits just "UPDATE T SET ".
    private static void EmitUpdate(SourceBuilder builder, PostgresUpdateModel m)
    {
        if (!m.HasEntityType || (m.EntityParamName is null))
        {
            SqlEmit.EmitCommandText(builder, "UPDATE " + Dialect.Quote(m.TableName) + " SET ");
            return;
        }

        var columns = m.Columns;
        var settable = columns.Where(static c => !c.IsKey && !c.IsDatabaseManaged).ToList();
        var keys = columns.Where(static c => c.IsKey).ToList();

        var sql = new StringBuilder();
        sql.Append("UPDATE ").Append(Dialect.Quote(m.TableName)).Append(" SET ");
        for (var i = 0; i < settable.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(Dialect.Quote(settable[i].ColumnName)).Append(" = ").Append(SqlEmit.Marker).Append(settable[i].PropertyName);
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
                sql.Append(Dialect.Quote(keys[i].ColumnName)).Append(" = ").Append(SqlEmit.Marker).Append("k_").Append(keys[i].PropertyName);
            }
        }

        sql.Append(ReturningClause(m.ReturningColumns));

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
    private static void EmitDelete(SourceBuilder builder, PostgresDeleteModel m)
    {
        var keyColumns = m.Columns.Where(static c => c.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(m);

        var sql = new StringBuilder();
        sql.Append("DELETE FROM ").Append(Dialect.Quote(m.TableName));
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
                sql.Append(Dialect.Quote(column)).Append(" = ").Append(SqlEmit.Marker).Append(bindParams[i].Name);
            }
        }

        sql.Append(ReturningClause(m.ReturningColumns));

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var p in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, p);
        }
    }

    // SELECT（全件）を組み立てる。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があればプロバイダのページング句を付ける。
    // Build a SELECT (all rows): SELECT * when there is no entity; otherwise list the columns and, if [Limit]/[Offset]
    // are present, append the provider's paging clause.
    private static void EmitSelect(SourceBuilder builder, PostgresSelectModel m)
    {
        if (!m.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Dialect.Quote(m.TableName));
            return;
        }

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(String.Join(", ", m.Columns.Select(c => Dialect.Quote(c.ColumnName)))).Append(" FROM ").Append(Dialect.Quote(m.TableName));

        // [Limit]/[Offset] パラメータがある場合のみ、プロバイダのページング句を付加する（パラメータ束縛は offset→limit の順）。
        // Append the provider's paging clause only when [Limit]/[Offset] parameters are present (params bound offset-then-limit).
        var valueParams = m.ValueParams;
        var limitParam = valueParams.FirstOrDefault(static p => p.IsLimit);
        var offsetParam = valueParams.FirstOrDefault(static p => p.IsOffset);
        if ((limitParam is not null) || (offsetParam is not null))
        {
            Dialect.AppendPaging(
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
    private static void EmitSelectSingle(SourceBuilder builder, PostgresSelectSingleModel m)
    {
        if (!m.HasEntityType)
        {
            SqlEmit.EmitCommandText(builder, "SELECT * FROM " + Dialect.Quote(m.TableName));
            return;
        }

        var keyColumns = m.Columns.Where(static c => c.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(m);

        var sql = new StringBuilder();
        sql.Append("SELECT ").Append(String.Join(", ", m.Columns.Select(c => Dialect.Quote(c.ColumnName)))).Append(" FROM ").Append(Dialect.Quote(m.TableName));
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
                sql.Append(Dialect.Quote(column)).Append(" = ").Append(SqlEmit.Marker).Append(bindParams[i].Name);
            }
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var p in bindParams)
        {
            SqlEmit.EmitValueParamBinding(builder, p);
        }
    }

    // INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col を組み立てる。INSERT 列は非 [DatabaseManaged]、突合は [Key]、
    // 更新は非キー・非 [DatabaseManaged] 列。更新対象が無ければ DO NOTHING。パラメータ束縛は INSERT エンティティモードと同じ。
    // Build INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col. INSERT columns are non-[DatabaseManaged]; the
    // conflict target is the [Key] columns; updates assign the non-key, non-[DatabaseManaged] columns. DO NOTHING when
    // there is nothing to update. Parameter binding is the same as INSERT entity mode.
    private static void EmitUpsert(SourceBuilder builder, PostgresUpsertModel m)
    {
        if (!m.HasEntityType || (m.EntityParamName is null))
        {
            // エンティティ実体が無く列を解決できない場合は何も組み立てない（診断は transform 側で報告済み）。
            // Without an entity instance the column list is unresolved; emit nothing (the diagnostic is raised in the transform).
            return;
        }

        var cols = m.Columns.Where(static c => !c.IsDatabaseManaged).ToList();
        var keys = m.Columns.Where(static c => c.IsKey).ToList();
        var updates = m.Columns.Where(static c => !c.IsKey && !c.IsDatabaseManaged).ToList();

        var colSql = String.Join(", ", cols.Select(c => Dialect.Quote(c.ColumnName)));
        var valSql = String.Join(", ", cols.Select(c => SqlEmit.Marker + c.PropertyName));
        var conflictSql = String.Join(", ", keys.Select(c => Dialect.Quote(c.ColumnName)));

        var sql = new StringBuilder();
        sql.Append("INSERT INTO ").Append(Dialect.Quote(m.TableName)).Append(" (").Append(colSql).Append(") VALUES (").Append(valSql).Append(") ON CONFLICT (").Append(conflictSql).Append(')');
        if (updates.Count > 0)
        {
            sql.Append(" DO UPDATE SET ");
            for (var i = 0; i < updates.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }
                sql.Append(Dialect.Quote(updates[i].ColumnName)).Append(" = EXCLUDED.").Append(Dialect.Quote(updates[i].ColumnName));
            }
        }
        else
        {
            sql.Append(" DO NOTHING");
        }

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var c in cols)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + c.PropertyName, $"{m.EntityParamName}.{c.PropertyName}", c);
        }
    }

    // 属性の Returning 名前引数（RETURNING 句で返す列。カンマ区切り）を読む。未指定・空白は null。
    // Read the attribute's Returning named argument (columns for the RETURNING clause, comma-separated). Null when absent/blank.
    private static string? ReadReturningColumns(AttributeData attr)
    {
        foreach (var kv in attr.NamedArguments)
        {
            if ((kv.Key == "Returning") && (kv.Value.Value is string s) && !String.IsNullOrWhiteSpace(s))
            {
                return s;
            }
        }
        return null;
    }

    // RETURNING 句を組み立てる。returningColumns（カンマ区切りの列名）を返す列として並べる。未指定なら空文字。
    // Build the RETURNING clause from returningColumns (comma-separated column names). Empty when absent.
    private static string ReturningClause(string? returningColumns)
    {
        if (returningColumns is null)
        {
            return string.Empty;
        }

        var parts = returningColumns.Split(',').Select(static c => c.Trim()).Where(static c => c.Length > 0).ToList();
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return " RETURNING " + String.Join(", ", parts.Select(Dialect.Quote));
    }
}
