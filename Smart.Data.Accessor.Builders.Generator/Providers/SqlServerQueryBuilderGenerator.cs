namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Smart.Data.Accessor.Builders.Generator.Engine;
using Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// SQL Server QueryBuilder ジェネレータ。[SqlInsert]/…/[SqlTruncate] が付いたメソッドに {Method}__QueryBuilder ヘルパーを
// 生成する（角括弧クォート、OFFSET/FETCH ページング）。[DataAccessor] で登録し、走査・出力スキャフォールド・SQL プリミティブは共有
// メカニクス（BuilderClassScanner / BuilderOutput / SqlEmit / MethodResolver）に委譲しつつ、種別の判定（SqlServerKind）・Model 構築・
// 各種別の SQL 組み立て（Execute）はこのプロバイダーが自前で持つ。
// The SQL Server QueryBuilder generator: emits the {Method}__QueryBuilder helper for methods carrying the
// [SqlInsert]/…/[SqlTruncate] attributes (bracket quoting, OFFSET/FETCH paging). Registers on [DataAccessor]
// and delegates scanning / output scaffolding / SQL primitives to the shared mechanics (BuilderClassScanner /
// BuilderOutput / SqlEmit / MethodResolver), while owning its own kind dispatch (SqlServerKind), model construction, and
// per-kind SQL assembly (Execute).
[Generator]
public sealed class SqlServerQueryBuilderGenerator : IIncrementalGenerator
{
    private const string Ns = "Smart.Data.Accessor.Attributes.SqlServer.Sql";

    private static readonly (string Attribute, SqlServerKind Kind)[] Targets =
    [
        (Ns + "InsertAttribute", SqlServerKind.Insert),
        (Ns + "UpdateAttribute", SqlServerKind.Update),
        (Ns + "DeleteAttribute", SqlServerKind.Delete),
        (Ns + "CountAttribute", SqlServerKind.Count),
        (Ns + "SelectAttribute", SqlServerKind.Select),
        (Ns + "SelectSingleAttribute", SqlServerKind.SelectSingle),
        (Ns + "TruncateAttribute", SqlServerKind.Truncate),
        (Ns + "MergeAttribute", SqlServerKind.Merge),
    ];

    private static readonly SqlDialect Dialect = new SqlServerDialect();

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

        context.RegisterSourceOutput(provider, static (spc, model) => BuilderOutput.Emit(spc, model, EmitMethod, ".SqlServer"));
    }

    // === Transform（種別判定と Model 構築。共通解決は MethodResolver に委譲） ===
    // === Transform (kind dispatch + model construction; common resolution is delegated to MethodResolver) ===

    // 1 つの QueryBuilder メソッドを Model 化する。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、
    // ここでは SqlServerKind 別の Model 生成と診断（キー欠如など）だけを行う。解決できない場合は null を返す。
    // Build the per-kind model for one QueryBuilder method. Common resolution (table / value params / columns) is
    // delegated to MethodResolver; only the SqlServerKind-specific model construction and diagnostics happen here.
    // Returns null when the method cannot be resolved.
    private static BuilderMethodModel? BuildMethod(MethodBuildContext<SqlServerKind> c)
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
            case SqlServerKind.Insert:
                return new SqlServerInsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, ReadOutputColumns(c.Attr));

            case SqlServerKind.Update:
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
                return new SqlServerUpdateModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, r.HasEntityType, ReadOutputColumns(c.Attr));

            case SqlServerKind.Delete:
                if (r.HasEntityType && !r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new SqlServerDeleteModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType, ReadOutputColumns(c.Attr));

            case SqlServerKind.Count:
                return new SqlServerCountModel(r.MethodName, r.TableName, r.ValueParams);

            case SqlServerKind.Truncate:
                return new SqlServerTruncateModel(r.MethodName, r.TableName, r.ValueParams);

            case SqlServerKind.Select:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                return new SqlServerSelectModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case SqlServerKind.SelectSingle:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new SqlServerSelectSingleModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case SqlServerKind.Merge:
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
                return new SqlServerMergeModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, r.HasEntityType);

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
            case SqlServerInsertModel m:
                EmitInsert(builder, m);
                break;
            case SqlServerUpdateModel m:
                EmitUpdate(builder, m);
                break;
            case SqlServerDeleteModel m:
                EmitDelete(builder, m);
                break;
            case SqlServerCountModel m:
                SqlEmit.EmitCommandText(builder, "SELECT COUNT(*) FROM " + Dialect.Quote(m.TableName));
                break;
            case SqlServerTruncateModel m:
                SqlEmit.EmitCommandText(builder, "TRUNCATE TABLE " + Dialect.Quote(m.TableName));
                break;
            case SqlServerSelectModel m:
                EmitSelect(builder, m);
                break;
            case SqlServerSelectSingleModel m:
                EmitSelectSingle(builder, m);
                break;
            case SqlServerMergeModel m:
                EmitMerge(builder, m);
                break;
        }

        SqlEmit.CloseMethod(builder);
    }

    // INSERT を組み立てる。エンティティモード（typeof(T) 指定）はエンティティ列（[DatabaseManaged] は除外）を、
    // パラメータモード（Table 指定）はバインドパラメータを列・値にする。
    // Build an INSERT. Entity mode (typeof(T)) uses the entity columns (excluding [DatabaseManaged]); parameter mode
    // (Table = "...") uses the bind parameters as columns/values.
    private static void EmitInsert(SourceBuilder builder, SqlServerInsertModel m)
    {
        if (m.EntityParamName is not null)
        {
            // エンティティモード：列はエンティティのプロパティから（DB が値を管理する [DatabaseManaged] 列は除外）。
            // Entity mode: columns from entity properties (excluding [DatabaseManaged], which the DB fills in).
            var cols = m.Columns.Where(static c => !c.IsDatabaseManaged).ToList();
            var colSql = String.Join(", ", cols.Select(c => Dialect.Quote(c.ColumnName)));
            var valSql = String.Join(", ", cols.Select(c => SqlEmit.Marker + c.PropertyName));
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Dialect.Quote(m.TableName)} ({colSql}){OutputClause(m.OutputColumns, "INSERTED")} VALUES ({valSql})");
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
            SqlEmit.EmitCommandText(builder, $"INSERT INTO {Dialect.Quote(m.TableName)} ({colSql}){OutputClause(m.OutputColumns, "INSERTED")} VALUES ({valSql})");
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
    private static void EmitUpdate(SourceBuilder builder, SqlServerUpdateModel m)
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
        sql.Append(OutputClause(m.OutputColumns, "INSERTED"));
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
    private static void EmitDelete(SourceBuilder builder, SqlServerDeleteModel m)
    {
        var keyColumns = m.Columns.Where(static c => c.IsKey).ToList();
        var bindParams = SqlEmit.BindParams(m);

        var sql = new StringBuilder();
        sql.Append("DELETE FROM ").Append(Dialect.Quote(m.TableName));
        sql.Append(OutputClause(m.OutputColumns, "DELETED"));
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

    // SELECT（全件）を組み立てる。エンティティが無ければ SELECT *。あれば列を明示し、[Limit]/[Offset] があればプロバイダのページング句を付ける。
    // Build a SELECT (all rows): SELECT * when there is no entity; otherwise list the columns and, if [Limit]/[Offset]
    // are present, append the provider's paging clause.
    private static void EmitSelect(SourceBuilder builder, SqlServerSelectModel m)
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
    private static void EmitSelectSingle(SourceBuilder builder, SqlServerSelectSingleModel m)
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

    // MERGE による UPSERT を組み立てる。USING で束縛値の仮想行 S を作り、[Key] で突合、WHEN MATCHED で非キー・非 [DatabaseManaged] 列を
    // 更新、WHEN NOT MATCHED で INSERT。更新列が無ければ WHEN MATCHED を省略。パラメータ束縛は INSERT エンティティモードと同じ。
    // Build a MERGE upsert: a source row S from the bound values, matched on [Key]; WHEN MATCHED updates the non-key,
    // non-[DatabaseManaged] columns and WHEN NOT MATCHED inserts. The WHEN MATCHED clause is omitted when nothing is
    // updatable. Parameter binding is the same as INSERT entity mode.
    private static void EmitMerge(SourceBuilder builder, SqlServerMergeModel m)
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

        var sql = new StringBuilder();
        sql.Append("MERGE INTO ").Append(Dialect.Quote(m.TableName)).Append(" AS T USING (SELECT ");
        for (var i = 0; i < cols.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(SqlEmit.Marker).Append(cols[i].PropertyName).Append(" AS ").Append(Dialect.Quote(cols[i].ColumnName));
        }
        sql.Append(") AS S ON (");
        for (var i = 0; i < keys.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(" AND ");
            }
            sql.Append("T.").Append(Dialect.Quote(keys[i].ColumnName)).Append(" = S.").Append(Dialect.Quote(keys[i].ColumnName));
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
                sql.Append("T.").Append(Dialect.Quote(updates[i].ColumnName)).Append(" = S.").Append(Dialect.Quote(updates[i].ColumnName));
            }
        }
        sql.Append(" WHEN NOT MATCHED THEN INSERT (");
        for (var i = 0; i < cols.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append(Dialect.Quote(cols[i].ColumnName));
        }
        sql.Append(") VALUES (");
        for (var i = 0; i < cols.Count; i++)
        {
            if (i > 0)
            {
                sql.Append(", ");
            }
            sql.Append("S.").Append(Dialect.Quote(cols[i].ColumnName));
        }
        sql.Append(");");

        SqlEmit.EmitCommandText(builder, sql.ToString());

        foreach (var c in cols)
        {
            SqlEmit.EmitColumnParameter(builder, SqlEmit.Marker + c.PropertyName, $"{m.EntityParamName}.{c.PropertyName}", c);
        }
    }

    // SqlServer 固有：属性の Output 名前引数（OUTPUT 句で返す列。カンマ区切り）を読む。未指定・空白は null。
    // SqlServer-specific: read the attribute's Output named argument (columns for the OUTPUT clause, comma-separated). Null when absent/blank.
    private static string? ReadOutputColumns(AttributeData attr)
    {
        foreach (var kv in attr.NamedArguments)
        {
            if ((kv.Key == "Output") && (kv.Value.Value is string s) && !String.IsNullOrWhiteSpace(s))
            {
                return s;
            }
        }
        return null;
    }

    // OUTPUT 句を組み立てる。outputColumns（カンマ区切りの列名）を pseudoTable（INSERTED / DELETED）の列として返す。未指定なら空文字。
    // Build the OUTPUT clause: render outputColumns (comma-separated column names) as columns of pseudoTable (INSERTED / DELETED). Empty when absent.
    private static string OutputClause(string? outputColumns, string pseudoTable)
    {
        if (outputColumns is null)
        {
            return string.Empty;
        }

        var parts = outputColumns.Split(',').Select(static c => c.Trim()).Where(static c => c.Length > 0).ToList();
        if (parts.Count == 0)
        {
            return string.Empty;
        }

        return " OUTPUT " + String.Join(", ", parts.Select(c => pseudoTable + "." + Dialect.Quote(c)));
    }
}
