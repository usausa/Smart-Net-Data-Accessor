namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SELECT COUNT(*) FROM <table>.
internal sealed record MySqlCountModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
