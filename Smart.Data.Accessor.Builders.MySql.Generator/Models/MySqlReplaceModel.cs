namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// REPLACE INTO（列・値は INSERT と同形）。
// REPLACE INTO (same column/value shape as INSERT).
internal sealed record MySqlReplaceModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
