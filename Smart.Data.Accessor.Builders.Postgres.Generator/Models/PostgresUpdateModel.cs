namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// UPDATE（RETURNING 句対応）。SET=非キー・非 [DatabaseManaged] 列、WHERE=[Key] 列。エンティティモードのみ。
// UPDATE (with RETURNING clause). SET = non-key, non-[DatabaseManaged] columns; WHERE = [Key] columns. Entity mode only.
internal sealed record PostgresUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    bool HasEntityType,
    string? ReturningColumns)
    : PostgresMethodModel(MethodName, TableName, ValueParams);
