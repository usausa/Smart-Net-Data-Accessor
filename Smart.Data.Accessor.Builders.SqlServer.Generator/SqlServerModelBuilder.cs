namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.SqlServer.Generator.Models;
using Smart.Data.Accessor.Shared.Builders;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// SQL Server Builder の transform。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、ここでは SqlServerOperation 別の
// Model 生成と診断（キー欠如など）、および SqlServer 固有の OUTPUT 列指定の読み取りだけを行う。解決できない場合は null を返す。
// Transform for the SQL Server builder. Common resolution (table / value params / columns) is delegated to
// MethodResolver; only the SqlServerOperation-specific model construction, diagnostics, and the SqlServer-specific OUTPUT
// column read happen here. Returns null when the method cannot be resolved.
internal static class SqlServerModelBuilder
{
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<SqlServerOperation> context)
    {
        var method = MethodResolver.Resolve(context.Container, context.Method, context.Attribute, context.TypeMaps, context.Profile, context.Diagnostics, context.Location);
        if (method is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle / Merge はキー（[Key]）が無いと WHERE/ON を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle / Merge need a key ([Key]) to build the WHERE/ON
        // clause, so they raise a diagnostic when none is present.
        switch (context.Operation)
        {
            case SqlServerOperation.Insert:
                return new SqlServerInsertModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, ReadOutputColumns(context.Attribute));

            case SqlServerOperation.Update:
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
                return new SqlServerUpdateModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, method.HasEntityType, ReadOutputColumns(context.Attribute));

            case SqlServerOperation.Delete:
                if (method.HasEntityType && !method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new SqlServerDeleteModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, ReadOutputColumns(context.Attribute));

            case SqlServerOperation.Count:
                return new SqlServerCountModel(method.MethodName, method.TableName, method.ValueParams);

            case SqlServerOperation.Truncate:
                return new SqlServerTruncateModel(method.MethodName, method.TableName, method.ValueParams);

            case SqlServerOperation.Select:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                return new SqlServerSelectModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            case SqlServerOperation.SelectSingle:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                else if (!method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new SqlServerSelectSingleModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            case SqlServerOperation.Merge:
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
                return new SqlServerMergeModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, method.HasEntityType);

            default:
                return null;
        }
    }

    // SqlServer 固有：属性の Output 名前引数（OUTPUT 句で返す列。カンマ区切り）を読む。未指定・空白は null。
    // SqlServer-specific: read the attribute's Output named argument (columns for the OUTPUT clause, comma-separated). Null when absent/blank.
    private static string? ReadOutputColumns(AttributeData attribute)
    {
        foreach (var namedArgument in attribute.NamedArguments)
        {
            if ((namedArgument.Key == "Output") && (namedArgument.Value.Value is string value) && !String.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }
        return null;
    }
}
