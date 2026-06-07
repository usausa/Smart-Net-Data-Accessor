namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SELECT <columns> FROM <table> [LIMIT/OFFSET ページング]。エンティティ必須。
// SELECT <columns> FROM <table> [LIMIT/OFFSET paging]. Entity required.
internal sealed record MySqlSelectModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    bool HasEntityType)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
