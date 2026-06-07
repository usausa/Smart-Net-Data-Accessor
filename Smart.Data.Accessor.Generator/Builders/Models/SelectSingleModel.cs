namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SELECT <columns> FROM <table> WHERE <バインドパラメータ。キー列に対応付け>。エンティティ必須。
// SELECT <columns> FROM <table> WHERE <bind params, keyed to key columns>. Entity required.
internal sealed record SelectSingleModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    bool HasEntityType)
    : StandardMethodModel(MethodName, TableName, ValueParams);
