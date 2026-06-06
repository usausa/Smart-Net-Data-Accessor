namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// DELETE FROM <table> WHERE <バインドパラメータ。エンティティ型がある場合はキー列に対応付け>。
// DELETE FROM <table> WHERE <bind params, keyed to key columns when an entity type is present>.
internal sealed record PostgresDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType,
    string? ReturningColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
