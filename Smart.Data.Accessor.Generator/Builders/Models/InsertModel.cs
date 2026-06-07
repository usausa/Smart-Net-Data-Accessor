namespace Smart.Data.Accessor.Generator.Builders.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// INSERT。EntityParamName があればエンティティモード（列は Columns、[DatabaseManaged] は除外）、無ければパラメータモード（列・値はバインドパラメータ）。
// INSERT. Entity mode when EntityParamName is set (columns from Columns, excluding [DatabaseManaged]); otherwise parameter mode (columns / values from the bind parameters).
internal sealed record InsertModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName)
    : StandardMethodModel(MethodName, TableName, ValueParams);
