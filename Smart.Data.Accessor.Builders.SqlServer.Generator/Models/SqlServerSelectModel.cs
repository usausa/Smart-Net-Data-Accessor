namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SELECT <columns> FROM <table> [OFFSET/FETCH ページング]。エンティティ必須。
// SELECT <columns> FROM <table> [OFFSET/FETCH paging]. Entity required.
internal sealed record SqlServerSelectModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    bool HasEntityType)
    : SqlServerMethodModel(MethodName, TableName, ValueParams);
