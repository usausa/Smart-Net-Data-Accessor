namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// INSERT IGNORE。列・値は INSERT と同形（エンティティモード／パラメータモード）。一意キー違反の行を黙って読み飛ばす。
// INSERT IGNORE. Same column/value shape as INSERT (entity / parameter mode); silently skips rows that violate a unique key.
internal sealed record MySqlInsertIgnoreModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
