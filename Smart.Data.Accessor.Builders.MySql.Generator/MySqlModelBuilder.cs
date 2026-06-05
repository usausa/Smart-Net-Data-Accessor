namespace Smart.Data.Accessor.Builders.MySql.Generator;

using Smart.Data.Accessor.Builders.GeneratorShared.Engine;
using Smart.Data.Accessor.Builders.GeneratorShared.Models;
using Smart.Data.Accessor.Builders.MySql.Generator.Models;

using SourceGenerateHelper;

// MySQL Builder の transform。共通解決（テーブル名・値パラメータ・列）は MethodResolver に委譲し、ここでは MySqlKind 別の Model
// 生成と診断（キー欠如など）だけを行う。解決できない場合は null を返す。
// Transform for the MySQL builder. Common resolution (table / value params / columns) is delegated to MethodResolver;
// only the MySqlKind-specific model construction and diagnostics happen here. Returns null when the method cannot be resolved.
internal static class MySqlModelBuilder
{
    public static BuilderMethodModel? BuildMethod(MethodBuildContext<MySqlKind> c)
    {
        var r = MethodResolver.Resolve(c.Container, c.Method, c.Attr, c.TypeMaps, c.Profile, c.Diagnostics, c.Location);
        if (r is null)
        {
            return null;
        }

        // 種別毎に対応する Model を返す。Update / Delete / SelectSingle / Upsert はキー（[Key]）が無いと WHERE/突合を組めないため診断を出す。
        // Return the model per kind. Update / Delete / SelectSingle / Upsert need a key ([Key]) to build the WHERE/match
        // clause, so they raise a diagnostic when none is present.
        switch (c.Kind)
        {
            case MySqlKind.Insert:
                return new MySqlInsertModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName);

            case MySqlKind.Update:
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

            case MySqlKind.Delete:
                if (r.HasEntityType && !r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new MySqlDeleteModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case MySqlKind.Count:
                return new MySqlCountModel(r.MethodName, r.TableName, r.ValueParams);

            case MySqlKind.Truncate:
                return new MySqlTruncateModel(r.MethodName, r.TableName, r.ValueParams);

            case MySqlKind.Select:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                return new MySqlSelectModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case MySqlKind.SelectSingle:
                if (!r.HasEntityType)
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.SelectColumnsUnresolvable, c.Location, c.Method.Name));
                }
                else if (!r.Columns.Any(static col => col.IsKey))
                {
                    c.Diagnostics.Add(new DiagnosticInfo(BuilderDiagnostics.NoKeyForBuilder, c.Location, r.EntityTypeName!, c.Method.Name));
                }
                return new MySqlSelectSingleModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.HasEntityType);

            case MySqlKind.Upsert:
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

            case MySqlKind.Replace:
                return new MySqlReplaceModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName);

            case MySqlKind.InsertIgnore:
                return new MySqlInsertIgnoreModel(r.MethodName, r.TableName, r.ValueParams, r.Columns, r.EntityParamName);

            default:
                return null;
        }
    }
}
