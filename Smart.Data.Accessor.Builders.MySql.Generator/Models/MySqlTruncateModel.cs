namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders.Models;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record MySqlTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
