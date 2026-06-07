namespace Smart.Data.Accessor.Builders.SqlServer.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// INSERT（OUTPUT 句対応）。EntityParamName があればエンティティモード、無ければパラメータモード。
// INSERT (with OUTPUT clause). Entity mode when EntityParamName is set; otherwise parameter mode.
internal sealed record SqlServerInsertModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName,
    string? OutputColumns)
    : SqlServerMethodModel(MethodName, TableName, ValueParams);
