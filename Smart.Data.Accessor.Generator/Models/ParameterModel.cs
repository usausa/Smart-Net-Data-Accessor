namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

internal enum ParameterDirectionKindLegacy
{
    Input,
    Output,
    InputOutput,
    ReturnValue
}

internal enum RefKindLegacy
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
    string? DbTypeExpr,     // e.g. "global::System.Data.DbType.AnsiString"
    int? Size,
    ParameterDirectionKindLegacy Direction,
    RefKindLegacy RefKind,
    string? EnumUnderlyingFullName,    // FQN of underlying primitive when parameter type is enum (or Nullable<enum>); null otherwise. spec §7.9
    bool IsNullableEnum,
    // spec §1.4 F15 / §5.3.1: provider-specific DbType from `[DbType<TEnum>(value)]`.
    // When non-null, emit `((ProviderParameterTypeFullName)p).ProviderPropertyName = ProviderValueExpr;`
    // after `AddInParameter`/`AddOutParameter`/`AddInOutParameter`.
    string? ProviderParameterTypeFullName,
    string? ProviderPropertyName,
    string? ProviderValueExpr,
    // spec §7.4 / §7.7: non-null when a [TypeHandler<>] applies to this parameter (member / method /
    // class / profile scope). The bound value is written via `TConverter.ToDb(value)`.
    // ConverterValueIsNullable is true when the parameter is `Nullable<TClr>` (a HasValue guard is
    // emitted so `ToDb` receives the non-null value and DB NULL is passed otherwise).
    string? ConverterTypeFullName = null,
    bool ConverterValueIsNullable = false,
    // spec §7.7 (改善2): the converter's IValueConverter<TDb, TClr> type-argument FQNs, for emitting
    // ExecuteHelper.AddInParameter<TConverter, TDb, TClr>. Meaningful only when ConverterTypeFullName is set.
    string? ConverterDbTypeFullName = null,
    string? ConverterClrTypeFullName = null,
    // spec §5.6: non-null when this parameter is a POCO argument on a [Procedure]/[DirectSql] method.
    // Its public properties are expanded into DB parameters; the parameter itself is not bound. The
    // method signature still declares the POCO argument.
    EquatableArray<PocoBindProperty>? PocoProperties = null,
    // spec §7.11 (P3) / SDA0510: public member names of this parameter's type (incl. inherited),
    // captured in the transform so the SQL stage can validate dotted /*@ root.Prop */ references
    // without symbols.
    EquatableArray<string> MemberNames = default);
