namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// MERGE による UPSERT。[Key] で突合し、WHEN MATCHED で非キー・非 [DatabaseManaged] 列を更新、WHEN NOT MATCHED で INSERT。エンティティモードのみ。
// MERGE-based UPSERT. Matches on [Key]; WHEN MATCHED updates the non-key, non-[DatabaseManaged] columns and WHEN NOT MATCHED inserts. Entity mode only.
internal sealed record SqlServerMergeModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : SqlServerMethodModel(MethodName, TableName, ValueParams);
