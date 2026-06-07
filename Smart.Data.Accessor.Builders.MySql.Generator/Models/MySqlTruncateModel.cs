namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>.
internal sealed record MySqlTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
