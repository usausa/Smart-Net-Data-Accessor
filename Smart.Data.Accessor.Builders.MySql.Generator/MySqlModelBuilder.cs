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
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<MySqlOperation> c)
    {
        var r = MethodResolver.Resolve(c.Container, c.Method, c.Attr, c.TypeMaps, c.Profile, c.Diagnostics, c.Location);
        if (r is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle / Upsert はキー（[Key]）が無いと WHERE/突合を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle / Upsert need a key ([Key]) to build the WHERE/match
        // clause, so they raise a diagnostic when none is present.
        switch (c.Operation)
        {
            case MySqlOperation.Insert:
                return new MySqlInsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName);

            case MySqlOperation.Update:
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
                return new MySqlUpdateModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, r.HasEntityType);

            case MySqlOperation.Delete:
                if (r.HasEntityType && !r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new MySqlDeleteModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case MySqlOperation.Count:
                return new MySqlCountModel(r.MethodName, r.TableName, r.ValueParams);

            case MySqlOperation.Truncate:
                return new MySqlTruncateModel(r.MethodName, r.TableName, r.ValueParams);

            case MySqlOperation.Select:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                return new MySqlSelectModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case MySqlOperation.SelectSingle:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new MySqlSelectSingleModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case MySqlOperation.Upsert:
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
                return new MySqlUpsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName, r.HasEntityType);

            case MySqlOperation.Replace:
                return new MySqlReplaceModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName);

            case MySqlOperation.InsertIgnore:
                return new MySqlInsertIgnoreModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName);

            default:
                return null;
        }
    }
}
