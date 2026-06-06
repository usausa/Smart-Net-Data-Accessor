namespace Smart.Data.Accessor.Builders.SqlServer.Generator;

using Microsoft.CodeAnalysis;

using Smart.Data.Accessor.Builders.SqlServer.Generator.Models;
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
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<SqlServerOperation> c)
    {
        var r = MethodResolver.Resolve(c.Container, c.Method, c.Attr, c.TypeMaps, c.Profile, c.Diagnostics, c.Location);
        if (r is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle / Merge はキー（[Key]）が無いと WHERE/ON を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle / Merge need a key ([Key]) to build the WHERE/ON
        // clause, so they raise a diagnostic when none is present.
        switch (c.Operation)
        {
            case SqlServerOperation.Insert:
                return new SqlServerInsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, ReadOutputColumns(c.Attr));

            case SqlServerOperation.Update:
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

            case SqlServerOperation.Delete:
                if (r.HasEntityType && !r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new SqlServerDeleteModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, ReadOutputColumns(c.Attr));

            case SqlServerOperation.Count:
                return new SqlServerCountModel(r.MethodName, r.TableName, r.ValueParams);

            case SqlServerOperation.Truncate:
                return new SqlServerTruncateModel(r.MethodName, r.TableName, r.ValueParams);

            case SqlServerOperation.Select:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                return new SqlServerSelectModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case SqlServerOperation.SelectSingle:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new SqlServerSelectSingleModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case SqlServerOperation.Merge:
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
}
