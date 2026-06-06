namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Builders.GeneratorShared.Models;

using SourceGenerateHelper;

// SELECT COUNT(*) FROM <table>。
// SELECT COUNT(*) FROM <table>.
internal sealed record MySqlCountModel(
    string MethodName,
    string TableName,
    EquatableArray<BuilderValueParam> ValueParams)
    : BuilderMethodModel(MethodName, TableName, ValueParams);
