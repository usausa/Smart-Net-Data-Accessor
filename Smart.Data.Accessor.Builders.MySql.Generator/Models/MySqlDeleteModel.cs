namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// DELETE FROM。WHERE=バインドパラメータ（[Key] 列に対応付け）。
// DELETE FROM. WHERE = bind parameters (mapped to key columns).
internal sealed record MySqlDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
