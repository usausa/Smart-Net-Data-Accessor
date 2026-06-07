namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// SELECT COUNT(*) FROM <table>.
internal sealed record CountModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams)
    : StandardMethodModel(MethodName, TableName, ValueParams);
