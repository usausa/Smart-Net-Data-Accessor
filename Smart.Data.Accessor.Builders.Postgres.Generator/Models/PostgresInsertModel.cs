namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// INSERT（RETURNING 句対応）。EntityParamName があればエンティティモード、無ければパラメータモード。
// INSERT (with RETURNING clause). Entity mode when EntityParamName is set; otherwise parameter mode.
internal sealed record PostgresInsertModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    string? ReturningColumns)
    : PostgresMethodModel(MethodName, TableName, ValueParams);
