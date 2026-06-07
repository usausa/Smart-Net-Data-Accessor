namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SELECT <columns> FROM <table> WHERE <[Key] 列に対応するバインドパラメータ>。エンティティ必須。
// SELECT <columns> FROM <table> WHERE <bind parameters mapped to the [Key] columns>. Entity required.
internal sealed record MySqlSelectSingleModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    bool HasEntityType)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
