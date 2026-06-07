namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>.
internal sealed record SqlServerTruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
    : SqlServerMethodModel(MethodName, TableName, ValueParams);
