namespace Smart.Data.Accessor.Generator.Builders;

using Smart.Data.Accessor.Generator.Builders.Models;
using Smart.Data.Accessor.Shared.Builders.Engine;
using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// 標準（既定）Builder の transform。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、ここでは Operation 別の
// Model 生成と診断（キー欠如など）だけを行う。解決できない場合は null を返す。
// Transform for the standard (default) builder. Common resolution (table / value params / columns) is delegated to
// MethodResolver; only the Operation-specific model construction and diagnostics happen here. Returns null when the method
// cannot be resolved.
internal static class StandardModelBuilder
{
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<Operation> context)
    {
        var method = MethodResolver.Resolve(context.Container, context.Method, context.Attribute, context.TypeMaps, context.Profile, context.Diagnostics, context.Location);
        if (method is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle はキー（[Key]）が無いと WHERE を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle need a key ([Key]) to build the WHERE clause, so they
        // raise a diagnostic when none is present.
        switch (context.Operation)
        {
            case Operation.Insert:
                return new InsertModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName);

            case Operation.Update:
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
                return new UpdateModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.EntityParamName, method.HasEntityType);

            case Operation.Delete:
                if (method.HasEntityType && !method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new DeleteModel(method.MethodName, method.TableName, method.ValueParams, method.Columns);

            case Operation.Count:
                return new CountModel(method.MethodName, method.TableName, method.ValueParams);

            case Operation.Truncate:
                return new TruncateModel(method.MethodName, method.TableName, method.ValueParams);

            case Operation.Select:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                return new SelectModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            case Operation.SelectSingle:
                if (!method.HasEntityType)
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, context.Location, context.Method.Name));
                }
                else if (!method.Columns.Any(static x => x.IsKey))
                {
                    context.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, context.Location, method.EntityTypeName!, context.Method.Name));
                }
                return new SelectSingleModel(method.MethodName, method.TableName, method.ValueParams, method.Columns, method.HasEntityType);

            default:
                return null;
        }
    }
}
