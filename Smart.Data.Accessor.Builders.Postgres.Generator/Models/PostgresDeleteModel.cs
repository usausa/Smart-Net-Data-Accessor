namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// DELETE（RETURNING 句対応）。WHERE=バインドパラメータ（[Key] 列に対応付け）。
// DELETE (with RETURNING clause). WHERE = bind parameters (mapped to key columns).
internal sealed record PostgresDeleteModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? ReturningColumns)
    : PostgresMethodModel(MethodName, TableName, ValueParams);
