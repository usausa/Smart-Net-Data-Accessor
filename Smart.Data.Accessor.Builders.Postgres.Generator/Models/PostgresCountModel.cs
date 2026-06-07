namespace Smart.Data.Accessor.Builders.Postgres.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SELECT COUNT(*) FROM <table>.
internal sealed record PostgresCountModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
    : PostgresMethodModel(MethodName, TableName, ValueParams);
