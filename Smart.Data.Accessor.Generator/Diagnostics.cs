namespace Smart.Data.Accessor.Generator;

using Microsoft.CodeAnalysis;

// Core DataAccessor generator diagnostics. IDs follow a phase-based banding aligned with the pipeline:
//   SDA00xx  class-level        (BuildClassResult: class structure + class attributes [Inject]/[Provider]/[ExecuteConfig])
//   SDA01xx  method structure   (method partial / command-source exclusivity A-group・B-group)
//   SDA02xx  parameter/direction(parameter attributes, [Direction], [DirectSql] param)
//   SDA03xx  return/mapping     (return-type shapes, reader, converter, [TypeHandler])
//   SDA04xx  SQL-file resolution(CompleteModel: SQL-file conflicts)
//   SDA05xx  2-way SQL parse    (BuildSqlEmitCode: tokenizer / pragma / parameter checks)
// Builder generator diagnostics use the SDA1xxx band (see BuilderDiagnostics).
// 2026-07-19 に全帯域を欠番なしへ再採番済み(リリース前の破壊的変更として実施。AnalyzerReleases 台帳は不採用)。
// Renumbered to gap-free bands on 2026-07-19 (a pre-release breaking change; the AnalyzerReleases ledger is not used).
internal static class Diagnostics
{
    // ==================================================================
    // SDA00xx — class-level
    // ==================================================================

    public static DiagnosticDescriptor InvalidClass { get; } = new(
        id: "SDA0001",
        title: "Invalid DataAccessor class",
        messageFormat: "Class must be declared as partial. class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DataAccessorClassNested { get; } = new(
        id: "SDA0002",
        title: "[DataAccessor] class must not be nested",
        messageFormat: "Class must not be nested. class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DataAccessorClassGeneric { get; } = new(
        id: "SDA0003",
        title: "[DataAccessor] class must not be generic",
        messageFormat: "Class must not be generic. class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InjectNameDuplicated { get; } = new(
        id: "SDA0004",
        title: "[Inject] Name is duplicated",
        messageFormat: "[Inject] Name is declared more than once. class=[{0}], name=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InjectNameConflictsWithMember { get; } = new(
        id: "SDA0005",
        title: "[Inject] Name conflicts with a member",
        messageFormat: "Name conflicts with an existing member. class=[{0}], name=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InjectTypeNotResolvable { get; } = new(
        id: "SDA0006",
        title: "[Inject] Type may not resolve",
        messageFormat: "Type may not resolve from IServiceProvider. class=[{0}], type=[{1}], name=[{2}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InjectNotReferenced { get; } = new(
        id: "SDA0007",
        title: "[Inject] declaration is not referenced",
        messageFormat: "[Inject] Name is never referenced. class=[{0}], name=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ProviderNameEmpty { get; } = new(
        id: "SDA0008",
        title: "[Provider] name is empty",
        messageFormat: "[Provider] has an empty name. class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ProviderOnPatternAOnlyAccessor { get; } = new(
        id: "SDA0009",
        title: "[Provider] has no effect",
        messageFormat: "Accessor has no Pattern B method. class=[{0}], name=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ExecuteConfigProfileInvalid { get; } = new(
        id: "SDA0010",
        title: "[ExecuteConfig] target is invalid",
        messageFormat: "Target type has no [AccessorProfile]. class=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ProfileCircularReference { get; } = new(
        id: "SDA0011",
        title: "Profile circular reference",
        messageFormat: "Profile class also has [ExecuteConfig]. class=[{0}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // [Naming] は assembly / class / method の全スコープに付くが、検証はコア Generator が一括して行うためこの帯に置く。
    // [Naming] can appear at assembly / class / method scope; the core generator validates all of them, so it lives in this band.
    public static DiagnosticDescriptor NamingValueUndefined { get; } = new(
        id: "SDA0012",
        title: "Undefined NamingConvention value",
        messageFormat: "NamingConvention value is undefined. value=[{0}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA01xx — method structure / command-source exclusivity
    // ==================================================================

    public static DiagnosticDescriptor InvalidMethod { get; } = new(
        id: "SDA0101",
        title: "Invalid DataAccessor method",
        messageFormat: "Method must be a partial declaration. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PartialMethodAlreadyImplemented { get; } = new(
        id: "SDA0102",
        title: "Partial implementation already exists",
        messageFormat: "A partial implementation is already present. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // The execution-kind attributes (A-group) are mutually exclusive.
    public static DiagnosticDescriptor ExecutionKindDuplicated { get; } = new(
        id: "SDA0103",
        title: "Multiple execution-kind attributes",
        messageFormat: "Multiple execution-kind attributes are present. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // [Procedure] and [DirectSql] (B-group command sources) are mutually exclusive. The QueryBuilder
    // combinations are SDA0105 (core) / SDA1002 (builder); this fills the remaining gap.
    public static DiagnosticDescriptor ProcedureDirectSqlConflict { get; } = new(
        id: "SDA0104",
        title: "[Procedure] combined with [DirectSql]",
        messageFormat: "Command source is ambiguous. method=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor BuilderAndCommandSourceConflict { get; } = new(
        id: "SDA0105",
        title: "Conflicting command source",
        messageFormat: "SQL source is ambiguous. method=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor MethodNameDuplicated { get; } = new(
        id: "SDA0106",
        title: "[MethodName] is duplicated",
        messageFormat: "[MethodName] is declared on multiple methods. class=[{0}], name=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlAndCommandSourceConflict { get; } = new(
        id: "SDA0107",
        title: "[Sql] conflicts with command source",
        messageFormat: "[Sql] cannot be combined with another source. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // 実行種別属性(A 群)は生成マーカーであり必須。コマンドソース属性(B 群)は実行種別を既定しない。
    // The execution-kind attribute (A-group) is the generation marker and mandatory; command-source
    // attributes (B-group) never default it.
    public static DiagnosticDescriptor ExecutionKindMissing { get; } = new(
        id: "SDA0108",
        title: "Execution-kind attribute required",
        messageFormat: "Execution-kind attribute is missing. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Query 形の CommandBehavior は F17 で固定(SingleResult 等)。reader 形だけが列読み順を呼出側が
    // 制御するため、behavior のオプトインを許す。
    // Query-shape behaviors are fixed by design (F17, SingleResult etc.); only the reader shape lets
    // the caller control the column read order, so only it accepts a behavior opt-in.
    public static DiagnosticDescriptor ReaderBehaviorInvalidMethod { get; } = new(
        id: "SDA0109",
        title: "[ReaderBehavior] on an invalid method",
        messageFormat: "[ReaderBehavior] needs [ExecuteReader]. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA02xx — parameter / direction
    // ==================================================================

    public static DiagnosticDescriptor NameDuplicated { get; } = new(
        id: "SDA0201",
        title: "Duplicate [Name]",
        messageFormat: "Multiple members share the same [Name]. method=[{0}], name=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DirectSqlFirstParamNotString { get; } = new(
        id: "SDA0202",
        title: "Invalid [DirectSql] first parameter",
        messageFormat: "First parameter must be string. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ProcedureNameEmpty { get; } = new(
        id: "SDA0203",
        title: "[Procedure] name is empty",
        messageFormat: "[Procedure] has an empty name. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AsyncProcedureRefParam { get; } = new(
        id: "SDA0204",
        title: "async [Procedure] with out/ref parameter",
        messageFormat: "async [Procedure] cannot use out/ref. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DbTypeAttributeConflict { get; } = new(
        id: "SDA0205",
        title: "Conflicting [DbType] attributes",
        messageFormat: "[DbType] and [DbType<TEnum>] conflict. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DbTypeProviderEnumNotWhitelisted { get; } = new(
        id: "SDA0206",
        title: "TEnum is not whitelisted",
        messageFormat: "TEnum [{2}] is not in the provider whitelist. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DirectionRefKindMismatch { get; } = new(
        id: "SDA0207",
        title: "[Direction] conflicts with the modifier",
        messageFormat: "[Direction({2})] conflicts with '{3}'. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DirectionOnUnsupportedMethod { get; } = new(
        id: "SDA0208",
        title: "[Direction] on unsupported method",
        messageFormat: "Method kind does not support [Direction]. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ReturnValueDirectionNotAllowed { get; } = new(
        id: "SDA0209",
        title: "[Direction(ReturnValue)] not supported",
        messageFormat: "[Direction(ReturnValue)] is not supported. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DirectSqlCommandTextDirection { get; } = new(
        id: "SDA0210",
        title: "[Direction] on command-text parameter",
        messageFormat: "[Direction] is not allowed here. method=[{0}], parameter=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlTextEmpty { get; } = new(
        id: "SDA0211",
        title: "[Sql] text is empty",
        messageFormat: "[Sql] has an empty SQL text. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA03xx — return shape / mapping / converter
    //
    // Mapping / converter validation is wired by ConverterResolver, invoked from
    // AccessorModelBuilder.BuildColumnInfos. Roslyn surfaces `static abstract` interface members as
    // IsAbstract && IsStatic; SDA0310 instead checks that the converter exposes accessible static
    // FromDb/ToDb (implicit interface implementation) so `TConverter.FromDb(...)` binds.
    // ==================================================================

    public static DiagnosticDescriptor UnsupportedReturn { get; } = new(
        id: "SDA0301",
        title: "Unsupported return type",
        messageFormat: "Return type is not supported. method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ExecuteReturnInvalid { get; } = new(
        id: "SDA0302",
        title: "Invalid [Execute] return type",
        messageFormat: "[Execute] return type is not supported. method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ExecuteReaderInvalidReturn { get; } = new(
        id: "SDA0303",
        title: "Invalid [ExecuteReader] return type",
        messageFormat: "Return type is not a data reader. method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ExecuteReaderRequiresUsing { get; } = new(
        id: "SDA0304",
        title: "[ExecuteReader] result needs disposal",
        messageFormat: "Caller must dispose the returned reader. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AsyncEnumerableMissingEnumeratorCancellation { get; } = new(
        id: "SDA0305",
        title: "Missing [EnumeratorCancellation]",
        messageFormat: "[EnumeratorCancellation] is missing. method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor RecordPrimaryConstructorPath { get; } = new(
        id: "SDA0306",
        title: "Record mapped via primary constructor",
        messageFormat: "Entity record uses positional binding. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NonNullableDbNull { get; } = new(
        id: "SDA0307",
        title: "Non-nullable property may get NULL",
        messageFormat: "Non-nullable property may receive DB NULL. method=[{0}], property=[{1}]",
        category: "Mapping",
        // Info: advisory only. As a warning it would fire on nearly every non-nullable
        // reference-type column and conflict with the project's zero-warning policy.
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ConverterTClrMismatch { get; } = new(
        id: "SDA0308",
        title: "Converter TClr mismatch",
        messageFormat: "Converter TClr does not match the property. method=[{0}], property=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ConverterNotIValueConverter { get; } = new(
        id: "SDA0309",
        title: "Invalid converter type",
        messageFormat: "Type does not implement IValueConverter<,>. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ConverterStaticAbstractMissing { get; } = new(
        id: "SDA0310",
        title: "Converter implementation is missing",
        messageFormat: "Static FromDb/ToDb implementation is missing. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypeHandlerDuplicated { get; } = new(
        id: "SDA0311",
        title: "Multiple [TypeHandler] on same property",
        messageFormat: "Only one [TypeHandler<>] is honored. method=[{0}], property=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor QueryElementHasNoMappableColumns { get; } = new(
        id: "SDA0312",
        title: "No mappable columns",
        messageFormat: "Query element type has no mappable column. method=[{0}], type=[{1}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA04xx — SQL-file resolution
    // ==================================================================

    public static DiagnosticDescriptor SqlNotFound { get; } = new(
        id: "SDA0401",
        title: "SQL file not found",
        messageFormat: "Neither a SQL file nor a Builder is specified. method=[{0}], file=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlFileNameCollision { get; } = new(
        id: "SDA0402",
        title: "SQL file name collision",
        messageFormat: "Multiple SQL files resolve to one name. method=[{0}], name=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor DirectSqlHasSqlFile { get; } = new(
        id: "SDA0403",
        title: "[DirectSql] has a SQL file",
        messageFormat: "[DirectSql] must not have a SQL file. method=[{0}], file=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor ProcedureHasSqlFile { get; } = new(
        id: "SDA0404",
        title: "[Procedure] has a SQL file",
        messageFormat: "[Procedure] must not have a SQL file. method=[{0}], file=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor BuilderAndSqlBothPresent { get; } = new(
        id: "SDA0405",
        title: "SQL file conflicts with QueryBuilder",
        messageFormat: "SQL file and QueryBuilder are both present. method=[{0}], file=[{1}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlHasSqlFile { get; } = new(
        id: "SDA0406",
        title: "[Sql] has a SQL file",
        messageFormat: "[Sql] must not have a SQL file. method=[{0}], file=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA05xx — 2-way SQL parse
    // ==================================================================

    public static DiagnosticDescriptor SqlTokenizeFailed { get; } = new(
        id: "SDA0501",
        title: "Failed to tokenize SQL",
        messageFormat: "SQL could not be tokenized. method=[{0}], detail=[{1}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlEmpty { get; } = new(
        id: "SDA0502",
        title: "SQL is empty",
        messageFormat: "SQL text is empty. method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlCommentNotClosed { get; } = new(
        id: "SDA0503",
        title: "SQL comment is not closed",
        messageFormat: "A SQL comment is not closed. method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlQuoteNotClosed { get; } = new(
        id: "SDA0504",
        title: "SQL quote is not closed",
        messageFormat: "A SQL string literal quote is not closed. method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlUnknownPragma { get; } = new(
        id: "SDA0505",
        title: "Unknown SQL pragma",
        messageFormat: "Unknown SQL pragma '/*!{1} */'. method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlCodeBlockBraceUnclosed { get; } = new(
        id: "SDA0506",
        title: "SQL code block has an unclosed brace",
        messageFormat: "Code block has an unclosed brace. method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlCodeBlockBraceExtraClose { get; } = new(
        id: "SDA0507",
        title: "SQL code block has an extra brace",
        messageFormat: "Code block has an unmatched closing brace. method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UndefinedSqlParameter { get; } = new(
        id: "SDA0508",
        title: "Undefined SQL parameter",
        messageFormat: "SQL parameter '@{1}' is not a method parameter. method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnusedMethodParameter { get; } = new(
        id: "SDA0509",
        title: "Method parameter is unused in SQL",
        messageFormat: "Method parameter is never used in SQL. method=[{0}], parameter=[{1}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor SqlPropertyNotFound { get; } = new(
        id: "SDA0510",
        title: "SQL property is not found",
        messageFormat: "Referenced property is not declared. method=[{0}], parameter=[{1}], property=[{2}], type=[{3}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
