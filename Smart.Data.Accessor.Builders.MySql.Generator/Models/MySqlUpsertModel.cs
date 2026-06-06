namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// INSERT ... ON DUPLICATE KEY UPDATE。INSERT 列は非 [DatabaseManaged]、ON DUPLICATE KEY UPDATE は非キー・非 [DatabaseManaged] 列。エンティティモードのみ。
// INSERT ... ON DUPLICATE KEY UPDATE. INSERT columns are non-[DatabaseManaged]; the update list is the non-key,
// non-[DatabaseManaged] columns. Entity mode only.
internal sealed record MySqlUpsertModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
