namespace Smart.Data.Accessor.Builders.Postgres.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.Postgres.Generator.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// PostgreSQL Builder の transform。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、ここでは PostgresOperation 別の
// Model 生成と診断（キー欠如など）、および Postgres 固有の RETURNING 列指定の読み取りだけを行う。解決できない場合は null を返す。
// Transform for the PostgreSQL builder. Common resolution (table / value params / columns) is delegated to
// MethodResolver; only the PostgresOperation-specific model construction, diagnostics, and the Postgres-specific RETURNING
// column read happen here. Returns null when the method cannot be resolved.
internal static class PostgresModelBuilder
{
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<PostgresOperation> context)
    {
        var method = MethodResolver.Resolve(context.Container, context.Method, context.Attribute, context.TypeMaps, context.Profile, context.Diagnostics, context.Location);
        if (method is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle / Upsert はキー（[Key]）が無いと WHERE/ON を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle / Upsert need a key ([Key]) to build the WHERE/ON
        // clause, so they raise a diagnostic when none is present.
        switch (context.Operation)
        {
            case PostgresOperation.Insert:
                return new PostgresInsertModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, ReadReturningColumns(context.Attribute));

            case PostgresOperation.Update:
                if (!method.HasEntityType || (method.EntityParamName is null))
                {
                    // SDA1004: 列リストを解決できない（エンティティ実体が無い）。
                    // SDA1004: cannot resolve the column list (no entity instance).
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                else if (!method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new PostgresUpdateModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, method.HasEntityType, ReadReturningColumns(context.Attribute));

            case PostgresOperation.Delete:
                if (method.HasEntityType && !method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new PostgresDeleteModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, ReadReturningColumns(context.Attribute));

            case PostgresOperation.Count:
                return new PostgresCountModel(method.MethodName, method.TableName, method.ValueParams);

            case PostgresOperation.Truncate:
                return new PostgresTruncateModel(method.MethodName, method.TableName, method.ValueParams);

            case PostgresOperation.Select:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                return new PostgresSelectModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            case PostgresOperation.SelectSingle:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                else if (!method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new PostgresSelectSingleModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            case PostgresOperation.Upsert:
                if (!method.HasEntityType || (method.EntityParamName is null))
                {
                    // SDA1004: 列リストを解決できない（エンティティ実体が無い）。
                    // SDA1004: cannot resolve the column list (no entity instance).
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                else if (!method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new PostgresUpsertModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, method.HasEntityType);

            default:
                return null;
        }
    }

    // 属性の Returning 名前引数（RETURNING 句で返す列。カンマ区切り）を読む。未指定・空白は null。
    // Read the attribute's Returning named argument (columns for the RETURNING clause, comma-separated). Null when absent/blank.
    private static string? ReadReturningColumns(AttributeData attribute)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if ((namedArgument.Key == "Returning") && (namedArgument.Value.Value is string value) && !String.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }
}
