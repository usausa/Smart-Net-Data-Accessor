namespace Smart.Data.Accessor.Builders.Generator;

using Smart.Data.Accessor.Builders.Generator.Models;
using Smart.Data.Accessor.Builders.GeneratorShared.Engine;
using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// 標準（既定）Builder の transform。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、ここでは Kind 別の
// Model 生成と診断（キー欠如など）だけを行う。解決できない場合は null を返す。
// Transform for the standard (default) builder. Common resolution (table / value params / columns) is delegated to
// MethodResolver; only the Kind-specific model construction and diagnostics happen here. Returns null when the method
// cannot be resolved.
internal static class StandardModelBuilder
{
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<Kind> c)
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
            case Kind.Insert:
                return new InsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName);

            case Kind.Update:
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
                return new UpdateModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, r.HasEntityType);

            case Kind.Delete:
                if (r.HasEntityType && !r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new DeleteModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case Kind.Count:
                return new CountModel(r.MethodName, r.TableName, r.ValueParams);

            case Kind.Truncate:
                return new TruncateModel(r.MethodName, r.TableName, r.ValueParams);

            case Kind.Select:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                return new SelectModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case Kind.SelectSingle:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new SelectSingleModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            default:
                return null;
        }
    }
}
