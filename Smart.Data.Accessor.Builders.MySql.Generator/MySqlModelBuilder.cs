namespace Smart.Data.Accessor.Builders.MySql.Generator;

using Smart.Data.Accessor.Builders.MySql.Generator.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// MySQL Builder の transform。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、ここでは MySqlOperation 別の Model
// 生成と診断（キー欠如など）だけを行う。解決できない場合は null を返す。
// Transform for the MySQL builder. Common resolution (table / value params / columns) is delegated to MethodResolver;
// only the MySqlOperation-specific model construction and diagnostics happen here. Returns null when the method cannot be resolved.
internal static class MySqlModelBuilder
{
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<MySqlOperation> context)
    {
        var method = MethodResolver.Resolve(context.Container, context.Method, context.Attribute, context.TypeMaps, context.Profile, context.Diagnostics, context.Location);
        if (method is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle / Upsert はキー（[Key]）が無いと WHERE/突合を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle / Upsert need a key ([Key]) to build the WHERE/match
        // clause, so they raise a diagnostic when none is present.
        switch (context.Operation)
        {
            case MySqlOperation.Insert:
                return new MySqlInsertModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName);

            case MySqlOperation.Update:
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
                return new MySqlUpdateModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, method.HasEntityType);

            case MySqlOperation.Delete:
                if (method.HasEntityType && !method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new MySqlDeleteModel(method.MethodName, method.TableName, method.ValueParams, method.Columns);

            case MySqlOperation.Count:
                return new MySqlCountModel(method.MethodName, method.TableName, method.ValueParams);

            case MySqlOperation.Truncate:
                return new MySqlTruncateModel(method.MethodName, method.TableName, method.ValueParams);

            case MySqlOperation.Select:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                return new MySqlSelectModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            case MySqlOperation.SelectSingle:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                else if (!method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new MySqlSelectSingleModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            case MySqlOperation.Upsert:
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
                return new MySqlUpsertModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, method.HasEntityType);

            case MySqlOperation.Replace:
                return new MySqlReplaceModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName);

            case MySqlOperation.InsertIgnore:
                return new MySqlInsertIgnoreModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName);

            default:
                return null;
        }
    }
}
