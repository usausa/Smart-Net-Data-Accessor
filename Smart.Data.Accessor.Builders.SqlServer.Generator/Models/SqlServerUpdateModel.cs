namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// UPDATE（OUTPUT 句対応）。SET=非キー・非 [DatabaseManaged] 列、WHERE=[Key] 列。エンティティモードのみ。
// UPDATE (with OUTPUT clause). SET = non-key, non-[DatabaseManaged] columns; WHERE = [Key] columns. Entity mode only.
internal sealed record SqlServerUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    bool HasEntityType,
    string? OutputColumns)
    : SqlServerMethodModel(MethodName, TableName, ValueParams);
