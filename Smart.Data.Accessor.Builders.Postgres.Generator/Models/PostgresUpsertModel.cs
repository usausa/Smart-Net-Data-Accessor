namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// INSERT ... ON CONFLICT (key) DO UPDATE。[Key] で突合し非キー・非 [DatabaseManaged] 列を更新。エンティティモードのみ。
// INSERT ... ON CONFLICT (key) DO UPDATE. Matches on [Key]; updates the non-key, non-[DatabaseManaged] columns. Entity mode only.
internal sealed record PostgresUpsertModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : PostgresMethodModel(MethodName, TableName, ValueParams);
