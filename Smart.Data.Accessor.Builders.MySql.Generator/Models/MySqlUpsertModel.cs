namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// INSERT ... ON DUPLICATE KEY UPDATE。[Key] で突合し非キー・非 [DatabaseManaged] 列を更新。エンティティモードのみ。
// INSERT ... ON DUPLICATE KEY UPDATE. Matches on [Key]; updates the non-key, non-[DatabaseManaged] columns. Entity mode only.
internal sealed record MySqlUpsertModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
