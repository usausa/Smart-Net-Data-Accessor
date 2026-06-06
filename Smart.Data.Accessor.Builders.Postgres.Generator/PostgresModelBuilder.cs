namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.GeneratorShared.Engine;
using Smart.Data.Accessor.Builders.GeneratorShared.Models;
using Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using SourceGenerateHelper;

// PostgreSQL Builder の transform。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、ここでは PostgresOperation 別の
// Model 生成と診断（キー欠如など）、および Postgres 固有の RETURNING 列指定の読み取りだけを行う。解決できない場合は null を返す。
// Transform for the PostgreSQL builder. Common resolution (table / value params / columns) is delegated to
// MethodResolver; only the PostgresOperation-specific model construction, diagnostics, and the Postgres-specific RETURNING
// column read happen here. Returns null when the method cannot be resolved.
internal static class PostgresModelBuilder
{
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<PostgresOperation> c)
    {
        var r = MethodResolver.Resolve(c.Container, c.Method, c.Attr, c.TypeMaps, c.Profile, c.Diagnostics, c.Location);
        if (r is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle / Upsert はキー（[Key]）が無いと WHERE/ON を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle / Upsert need a key ([Key]) to build the WHERE/ON
        // clause, so they raise a diagnostic when none is present.
        switch (c.Operation)
        {
            case PostgresOperation.Insert:
                return new PostgresInsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, ReadReturningColumns(c.Attr));

            case PostgresOperation.Update:
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

            case PostgresOperation.Delete:
                if (r.HasEntityType && !r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new PostgresDeleteModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType, ReadReturningColumns(c.Attr));

            case PostgresOperation.Count:
                return new PostgresCountModel(r.MethodName, r.TableName, r.ValueParams);

            case PostgresOperation.Truncate:
                return new PostgresTruncateModel(r.MethodName, r.TableName, r.ValueParams);

            case PostgresOperation.Select:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                return new PostgresSelectModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case PostgresOperation.SelectSingle:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new PostgresSelectSingleModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case PostgresOperation.Upsert:
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
}
