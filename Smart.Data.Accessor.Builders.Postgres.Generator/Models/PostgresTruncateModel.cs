namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>.
internal sealed record PostgresTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
    : PostgresMethodModel(MethodName, TableName, ValueParams);
