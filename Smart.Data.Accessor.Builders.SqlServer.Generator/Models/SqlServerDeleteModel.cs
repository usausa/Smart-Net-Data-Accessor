namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// DELETE FROM <table> WHERE <バインドパラメータ。エンティティ型がある場合はキー列に対応付け>。
// DELETE FROM <table> WHERE <bind params, keyed to key columns when an entity type is present>.
internal sealed record SqlServerDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType,
    string? OutputColumns)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
