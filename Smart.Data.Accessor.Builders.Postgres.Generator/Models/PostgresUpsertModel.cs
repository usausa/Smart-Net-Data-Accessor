namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col（更新対象が無ければ DO NOTHING）。[Key] で突合、非キー・非 [DatabaseManaged] 列を更新。エンティティモードのみ。
// INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col (DO NOTHING when nothing is updatable). Matches on
// [Key] columns; updates the non-key, non-[DatabaseManaged] columns. Entity mode only.
internal sealed record PostgresUpsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
