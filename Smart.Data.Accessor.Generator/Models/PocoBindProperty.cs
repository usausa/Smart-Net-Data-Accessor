namespace Smart.Data.Accessor.Generator.Models;

// a property of a POCO procedure/DirectSql argument, expanded into one DB parameter.
// Input by default; [Direction(Output/InputOutput)] makes it an output written back to
// {ArgName}.{PropertyName}. The value is read from {ArgName}.{PropertyName}.
internal sealed record PocoBindProperty(
    string PropertyName,
    string ParamName,                  // DB parameter name (BindMarker is prepended at emit): [Name] or PropertyName
    string TypeFullName,
    ParameterDirectionType Direction,
    string? DbTypeExpression,
    int? Size,
    string? EnumUnderlyingFullName,
    bool IsNullableEnum,
    string HandleName,                 // __op_{ArgName}_{PropertyName} for Output / InputOutput
    // when set, input is written via TConverter.ToDb and OUT read as ConverterDbTypeFullName (= TDb)
    // then TConverter.FromDb. ConverterClrTypeFullName (= TClr) + ConverterDbTypeFullName feed
    // ExecuteHelper.AddInParameter<TConverter,TDb,TClr>. ConverterValueIsNullable adds a HasValue
    // guard for a Nullable<TClr> input.
    string? ConverterTypeFullName = null,
    string? ConverterDbTypeFullName = null,
    string? ConverterClrTypeFullName = null,
    bool ConverterValueIsNullable = false);
