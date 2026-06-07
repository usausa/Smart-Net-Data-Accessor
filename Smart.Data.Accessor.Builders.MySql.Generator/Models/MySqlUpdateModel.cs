namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// UPDATE。SET=非キー・非 [DatabaseManaged] 列、WHERE=[Key] 列。エンティティモードのみ。
// UPDATE. SET = non-key, non-[DatabaseManaged] columns; WHERE = [Key] columns. Entity mode only.
internal sealed record MySqlUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
