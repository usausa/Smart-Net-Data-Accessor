namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// REPLACE INTO。列・値は INSERT と同形（エンティティモード／パラメータモード）。重複キーで delete→insert。
// REPLACE INTO. Same column/value shape as INSERT (entity / parameter mode); deletes-then-inserts on a duplicate key.
internal sealed record MySqlReplaceModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
