namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// MERGE による UPSERT。[Key] 列で突合し、WHEN MATCHED で非キー・非 [DatabaseManaged] 列を更新、WHEN NOT MATCHED で INSERT。エンティティモードのみ。
// MERGE-based UPSERT. Matches on [Key] columns; WHEN MATCHED updates the non-key, non-[DatabaseManaged]
// columns and WHEN NOT MATCHED inserts. Entity mode only.
internal sealed record SqlServerMergeModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
