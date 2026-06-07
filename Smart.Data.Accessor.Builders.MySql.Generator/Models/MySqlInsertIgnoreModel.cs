namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// INSERT IGNORE INTO（列・値は INSERT と同形）。
// INSERT IGNORE INTO (same column/value shape as INSERT).
internal sealed record MySqlInsertIgnoreModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
