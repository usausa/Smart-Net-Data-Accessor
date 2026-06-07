namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// DELETE（OUTPUT 句対応）。WHERE=バインドパラメータ（[Key] 列に対応付け）。
// DELETE (with OUTPUT clause). WHERE = bind parameters (mapped to key columns).
internal sealed record SqlServerDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? OutputColumns)
    : SqlServerMethodModel(MethodName, TableName, ValueParams);
