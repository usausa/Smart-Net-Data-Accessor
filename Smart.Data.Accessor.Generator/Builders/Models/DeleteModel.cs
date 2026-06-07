namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// DELETE FROM <table> WHERE <バインドパラメータ。エンティティ型がある場合はキー列に対応付け>。
// DELETE FROM <table> WHERE <bind params, keyed to key columns when an entity type is present>.
internal sealed record DeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns)
    : StandardMethodModel(MethodName, TableName, ValueParams);
