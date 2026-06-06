namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record PostgresTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
