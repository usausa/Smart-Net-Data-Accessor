namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>。
// TRUNCATE TABLE <table>.
internal sealed record TruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
