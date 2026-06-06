namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// SELECT <columns> FROM <table> [プロバイダーのページング]。エンティティ必須、ページングは [Limit]/[Offset] パラメータから。
// SELECT <columns> FROM <table> [provider paging]. Entity required; paging from [Limit]/[Offset] params.
internal sealed record SqlServerSelectModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams,
    EquatableArray<BuilderColumn> Columns,
    bool HasEntityType)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
