namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// UPDATE <table> SET <非キー・非 [DatabaseManaged] 列> WHERE <キー列>。エンティティモードのみ。
// UPDATE <table> SET <non-key, non-[DatabaseManaged] columns> WHERE <key columns>. Entity mode only.
internal sealed record UpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : StandardMethodModel(MethodName, TableName, ValueParams);
