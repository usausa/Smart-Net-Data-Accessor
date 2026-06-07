namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// TRUNCATE TABLE <table>.
internal sealed record TruncateModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
    : StandardMethodModel(MethodName, TableName, ValueParams);
