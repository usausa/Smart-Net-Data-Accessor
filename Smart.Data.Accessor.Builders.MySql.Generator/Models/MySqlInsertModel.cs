namespace Smart.Data.Accessor.Builders.MySql.Generator.Models;

using Smart.Data.Accessor.Shared.Builders;

using SourceGenerateHelper;

// INSERT INTO。EntityParamName があればエンティティモード、無ければパラメータモード。
// INSERT INTO. Entity mode when EntityParamName is set; otherwise parameter mode.
internal sealed record MySqlInsertModel(
    string MethodName,
    string TableName,
    EquatableArray<ParameterBinding> ValueParams,
    EquatableArray<ColumnBinding> Columns,
    string? EntityParamName)
    : MySqlMethodModel(MethodName, TableName, ValueParams);
