namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record SqlServerTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
