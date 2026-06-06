namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

internal enum ParameterDirectionType
{
    Input,
    Output,
    InputOutput,
    ReturnValue
}

internal enum ParameterRefKind
{
    None,
    Out,
    Ref
}

internal sealed record ParameterModel(
    string Name,
    string TypeFullName,
    bool IsNullable,
    bool IsCancellationToken,
    bool IsDbConnection,
    bool IsDbTransaction,
    ParameterDirectionType Direction,
    ParameterRefKind RefKind,
    string? DbTypeExpr,     // e.g. "global::System.Data.DbType.AnsiString"
    int? Size,
    string? EnumUnderlyingFullName,    // FQN of underlying primitive when parameter type is enum (or Nullable<enum>); null otherwise
    bool IsNullableEnum,
    // provider-specific DbType from `[DbType<TEnum>(value)]`.
    // When non-null, emit `((ProviderParameterTypeFullName)p).ProviderPropertyName = ProviderValueExpr;`
    // after `AddInParameter`/`AddOutParameter`/`AddInOutParameter`.
    string? ProviderParameterTypeFullName,
    string? ProviderPropertyName,
    string? ProviderValueExpr,
    // non-null when a [TypeHandler<>] applies to this parameter (member / method / class / profile
    // scope). The bound value is written via `TConverter.ToDb(value)`. ConverterValueIsNullable is
    // true when the parameter is `Nullable<TClr>` (a HasValue guard is emitted so `ToDb` receives the
    // non-null value and DB NULL is passed otherwise).
    string? ConverterTypeFullName = null,
    bool ConverterValueIsNullable = false,
    // the converter's IValueConverter<TDb, TClr> type-argument FQNs, for emitting
    // ExecuteHelper.AddInParameter<TConverter, TDb, TClr>. Meaningful only when ConverterTypeFullName is set.
    string? ConverterDbTypeFullName = null,
    string? ConverterClrTypeFullName = null,
    // non-null when this parameter is a POCO argument on a [Procedure]/[DirectSql] method.
    // Its public properties are expanded into DB parameters; the parameter itself is not bound. The
    // method signature still declares the POCO argument.
    EquatableArray<PocoBindProperty>? PocoProperties = null,
    // SDA0510: public member names of this parameter's type (incl. inherited), captured in the
    // transform so the SQL stage can validate dotted /*@ root.Prop */ references without symbols.
    EquatableArray<string> MemberNames = default);
