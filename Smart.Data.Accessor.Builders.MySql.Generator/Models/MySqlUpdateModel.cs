namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// UPDATE <table> SET <非キー・非 [DatabaseManaged] 列> WHERE <キー列>。エンティティモードのみ。
// UPDATE <table> SET <non-key, non-[DatabaseManaged] columns> WHERE <key columns>. Entity mode only.
internal sealed record MySqlUpdateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    string? EntityParamName,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
