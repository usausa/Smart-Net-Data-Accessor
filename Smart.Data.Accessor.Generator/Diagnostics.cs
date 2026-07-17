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

    public static readonly DiagnosticDescriptor InvalidClass = new(
        id: "SDA0001",
        title: "Invalid DataAccessor class",
        messageFormat: "DataAccessor class must be declared as partial. class=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAccessorClassNested = new(
        id: "SDA0002",
        title: "[DataAccessor] class must not be nested",
        messageFormat: "[DataAccessor] class must be top-level and must not be nested. class=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAccessorClassGeneric = new(
        id: "SDA0003",
        title: "[DataAccessor] class must not be generic",
        messageFormat: "[DataAccessor] class must not be generic. class=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNameDuplicated = new(
        id: "SDA0004",
        title: "[Inject] Name is duplicated within the class",
        messageFormat: "[Inject] Name is declared more than once. class=[{0}], name=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNameConflictsWithMember = new(
        id: "SDA0005",
        title: "[Inject] Name conflicts with another member or method parameter",
        messageFormat: "[Inject] Name conflicts with an existing field, property, or method parameter. class=[{0}], name=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectTypeNotResolvable = new(
        id: "SDA0006",
        title: "[Inject] Type may not resolve from IServiceProvider",
        messageFormat: "[Inject] declares a type that may not be resolvable from IServiceProvider, and runtime will throw if it is unregistered. class=[{0}], type=[{1}], name=[{2}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNotReferenced = new(
        id: "SDA0007",
        title: "[Inject] declaration is not referenced",
        messageFormat: "[Inject] Name is referenced neither in SQL (/*@ {1}.X */ etc.) nor in user code. class=[{0}], name=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProviderNameEmpty = new(
        id: "SDA0008",
        title: "[Provider] name is empty",
        messageFormat: "[Provider] has an empty name, so IDbProviderSelector.GetProvider will receive an empty string. class=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProviderOnPatternAOnlyAccessor = new(
        id: "SDA0009",
        title: "[Provider] has no effect on Pattern A only accessor",
        messageFormat: "[Provider] is set but the accessor has no Pattern B methods, so the name is never passed to IDbProviderSelector.GetProvider. class=[{0}], name=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteConfigProfileInvalid = new(
        id: "SDA0010",
        title: "[ExecuteConfig] target is not an [AccessorProfile]",
        messageFormat: "[ExecuteConfig] target type does not have [AccessorProfile]. class=[{0}], type=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProfileCircularReference = new(
        id: "SDA0011",
        title: "[AccessorProfile] class also has [ExecuteConfig]",
        messageFormat: "[AccessorProfile] class also has [ExecuteConfig], creating a circular reference. class=[{0}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA01xx — method structure / command-source exclusivity
    // ==================================================================

    public static readonly DiagnosticDescriptor InvalidMethod = new(
        id: "SDA0101",
        title: "Invalid DataAccessor method",
        messageFormat: "DataAccessor method must be a partial declaration. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PartialMethodAlreadyImplemented = new(
        id: "SDA0102",
        title: "Partial method implementation already exists",
        messageFormat: "A partial implementation is already present in source, so the generator cannot emit a second implementation. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // The execution-kind attributes (A-group) are mutually exclusive.
    public static readonly DiagnosticDescriptor ExecutionKindDuplicated = new(
        id: "SDA0103",
        title: "Multiple execution-kind attributes on the same method",
        messageFormat: "More than one execution attribute ([Execute]/[ExecuteScalar]/[Query]/[QueryFirst]/[ExecuteReader]) is present; they are mutually exclusive. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // [Procedure] and [DirectSql] (B-group command sources) are mutually exclusive. The QueryBuilder
    // combinations are SDA0105 (core) / SDA1002 (builder); this fills the remaining gap.
    public static readonly DiagnosticDescriptor ProcedureDirectSqlConflict = new(
        id: "SDA0104",
        title: "[Procedure] combined with [DirectSql]",
        messageFormat: "[Procedure] cannot be combined with [DirectSql]; the command source is ambiguous. method=[{0}].",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderAndCommandSourceConflict = new(
        id: "SDA0105",
        title: "QueryBuilder attribute combined with [Procedure] / [DirectSql]",
        messageFormat: "A QueryBuilder attribute ([Insert]/[Update]/…) cannot be combined with [Procedure] or [DirectSql]; the SQL source is ambiguous. method=[{0}].",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodNameDuplicated = new(
        id: "SDA0106",
        title: "[MethodName] is duplicated within the class",
        messageFormat: "[MethodName] is declared on multiple methods and would collide in SQL-file lookup. class=[{0}], name=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlAndCommandSourceConflict = new(
        id: "SDA0107",
        title: "[Sql] cannot be combined with another command source",
        messageFormat: "[Sql] cannot be combined with another command-source attribute ([DirectSql] / [Procedure] / QueryBuilder). method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // 実行種別属性(A 群)は生成マーカーであり必須。コマンドソース属性(B 群)は実行種別を既定しない。
    // The execution-kind attribute (A-group) is the generation marker and mandatory; command-source
    // attributes (B-group) never default it.
    public static readonly DiagnosticDescriptor ExecutionKindMissing = new(
        id: "SDA0108",
        title: "Execution-kind attribute required",
        messageFormat: "A command-source attribute ([Sql] / [DirectSql] / [Procedure] / QueryBuilder) is present but the execution-kind attribute ([Execute] / [ExecuteScalar] / [ExecuteReader] / [Query] / [QueryFirst]) is missing. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Query 形の CommandBehavior は F17 で固定(SingleResult 等)。reader 形だけが列読み順を呼出側が
    // 制御するため、behavior のオプトインを許す。
    // Query-shape behaviors are fixed by design (F17, SingleResult etc.); only the reader shape lets
    // the caller control the column read order, so only it accepts a behavior opt-in.
    public static readonly DiagnosticDescriptor ReaderBehaviorInvalidMethod = new(
        id: "SDA0109",
        title: "[ReaderBehavior] is only valid on [ExecuteReader] methods",
        messageFormat: "[ReaderBehavior] is only valid on an [ExecuteReader] method (Query-shape behaviors are fixed by design). method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA02xx — parameter / direction
    // ==================================================================

    public static readonly DiagnosticDescriptor NameDuplicated = new(
        id: "SDA0201",
        title: "Duplicate [Name] on parameters or properties",
        messageFormat: "Multiple parameters or properties share the same [Name]. method=[{0}], name=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlFirstParamNotString = new(
        id: "SDA0202",
        title: "[DirectSql] method first parameter must be string",
        messageFormat: "[DirectSql] requires the first parameter (after conn/tx/CancellationToken) to be `string` for the command text. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProcedureNameEmpty = new(
        id: "SDA0203",
        title: "[Procedure] stored procedure name is empty",
        messageFormat: "[Procedure] has an empty stored procedure name. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AsyncProcedureRefParam = new(
        id: "SDA0204",
        title: "async [Procedure] cannot use out/ref parameters",
        messageFormat: "async [Procedure] cannot use an out/ref parameter; switch to a synchronous method or POCO aggregation. method=[{0}], parameter=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DbTypeAttributeConflict = new(
        id: "SDA0205",
        title: "Conflicting [DbType] / [DbType<TEnum>] on same parameter",
        messageFormat: "Parameter has both [DbType(DbType)] and [DbType<TEnum>], which conflict. method=[{0}], parameter=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DbTypeProviderEnumNotWhitelisted = new(
        id: "SDA0206",
        title: "[DbType<TEnum>] TEnum is not in the provider enum whitelist",
        messageFormat: "Parameter uses [DbType<{2}>] where TEnum is not in the provider enum whitelist; the provider-specific DbType assignment will be skipped. method=[{0}], parameter=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectionRefKindMismatch = new(
        id: "SDA0207",
        title: "[Direction] conflicts with the parameter modifier",
        messageFormat: "[Direction({2})] conflicts with the parameter modifier '{3}'. method=[{0}], parameter=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectionOnUnsupportedMethod = new(
        id: "SDA0208",
        title: "[Direction] used on unsupported method kind",
        messageFormat: "Parameter has [Direction] but the method is not [Procedure] / [Execute] / [DirectSql]. method=[{0}], parameter=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReturnValueDirectionNotAllowed = new(
        id: "SDA0209",
        title: "[Direction(ReturnValue)] is not supported",
        messageFormat: "Parameter cannot use [Direction(ReturnValue)]; the stored procedure RETURN value maps to the method's scalar return value. method=[{0}], parameter=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlCommandTextDirection = new(
        id: "SDA0210",
        title: "[Direction] not allowed on [DirectSql] command-text parameter",
        messageFormat: "The command-text string parameter on a [DirectSql] method cannot be annotated with [Direction]. method=[{0}], parameter=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlTextEmpty = new(
        id: "SDA0211",
        title: "[Sql] text is empty",
        messageFormat: "[Sql] has an empty SQL text. method=[{0}].",
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

    public static readonly DiagnosticDescriptor UnsupportedReturn = new(
        id: "SDA0301",
        title: "Unsupported return type",
        messageFormat: "Return type is not supported. method=[{0}], type=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReturnInvalid = new(
        id: "SDA0302",
        title: "[Execute] return type must be int/void/Task<int>/Task",
        messageFormat: "[Execute] return type is not one of int/void/Task<int>/Task/ValueTask<int>/ValueTask. method=[{0}], type=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReaderInvalidReturn = new(
        id: "SDA0303",
        title: "[ExecuteReader] return type is not a reader",
        messageFormat: "[ExecuteReader] return type is not IDataReader / DbDataReader / Task<...> / ValueTask<...>. method=[{0}], type=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReaderRequiresUsing = new(
        id: "SDA0304",
        title: "[ExecuteReader] result must be disposed by the caller",
        messageFormat: "[ExecuteReader] returns a reader that owns its command (and connection for Pattern B); callers must dispose it with `using`. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AsyncEnumerableMissingEnumeratorCancellation = new(
        id: "SDA0305",
        title: "IAsyncEnumerable method requires [EnumeratorCancellation] CancellationToken",
        messageFormat: "IAsyncEnumerable<T> method should declare a CancellationToken parameter annotated with [EnumeratorCancellation]. method=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RecordPrimaryConstructorPath = new(
        id: "SDA0306",
        title: "Record entity is mapped via primary constructor",
        messageFormat: "Entity record is mapped via its primary constructor (positional binding). method=[{0}], type=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonNullableDbNull = new(
        id: "SDA0307",
        title: "Non-nullable property may receive DB NULL",
        messageFormat: "Property is a non-nullable reference type; DB NULL will fall through as default! (null). method=[{0}], property=[{1}].",
        category: "Mapping",
        // Info: advisory only. As a warning it would fire on nearly every non-nullable
        // reference-type column and conflict with the project's zero-warning policy.
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterTClrMismatch = new(
        id: "SDA0308",
        title: "Converter TClr does not match property type",
        messageFormat: "Converter declares a TClr that does not match the property type. method=[{0}], property=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterNotIValueConverter = new(
        id: "SDA0309",
        title: "Converter type does not implement IValueConverter<,>",
        messageFormat: "Converter type does not implement IValueConverter<TDb, TClr>. method=[{0}], type=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterStaticAbstractMissing = new(
        id: "SDA0310",
        title: "Converter type missing static abstract implementation",
        messageFormat: "Converter type does not provide a static implementation of FromDb/ToDb. method=[{0}], type=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeHandlerDuplicated = new(
        id: "SDA0311",
        title: "Multiple [TypeHandler] on same property",
        messageFormat: "Property has multiple [TypeHandler<>] attributes; only one converter is honored. method=[{0}], property=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryElementHasNoMappableColumns = new(
        id: "SDA0312",
        title: "Query element type has no mappable columns",
        messageFormat: "Query element type has no mappable columns (public settable/init property or record primary-constructor parameter). method=[{0}], type=[{1}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA04xx — SQL-file resolution
    // ==================================================================

    public static readonly DiagnosticDescriptor SqlNotFound = new(
        id: "SDA0401",
        title: "SQL file not found",
        messageFormat: "Neither a SQL file nor a Builder is specified; an additional file is expected. method=[{0}], file=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlFileNameCollision = new(
        id: "SDA0402",
        title: "SQL file name collision",
        messageFormat: "Multiple SQL files resolve to the same logical name. method=[{0}], name=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlHasSqlFile = new(
        id: "SDA0403",
        title: "[DirectSql] method must not have a corresponding SQL file",
        messageFormat: "[DirectSql] method must not have a corresponding SQL file. method=[{0}], file=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProcedureHasSqlFile = new(
        id: "SDA0404",
        title: "[Procedure] method must not have a corresponding SQL file",
        messageFormat: "[Procedure] is set but a corresponding SQL file also exists. method=[{0}], file=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderAndSqlBothPresent = new(
        id: "SDA0405",
        title: "Both SQL file and a QueryBuilder attribute are present",
        messageFormat: "Both a SQL file and a QueryBuilder attribute ([Insert]/[Update]/…) are present; resolution is ambiguous. method=[{0}], file=[{1}].",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlHasSqlFile = new(
        id: "SDA0406",
        title: "[Sql] method must not have a corresponding SQL file",
        messageFormat: "[Sql] is set but a corresponding SQL file also exists. method=[{0}], file=[{1}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA05xx — 2-way SQL parse
    // ==================================================================

    public static readonly DiagnosticDescriptor SqlTokenizeFailed = new(
        id: "SDA0501",
        title: "Failed to tokenize SQL",
        messageFormat: "Failed to tokenize SQL. method=[{0}], detail=[{1}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlEmpty = new(
        id: "SDA0502",
        title: "SQL is empty",
        messageFormat: "SQL is empty. method=[{0}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlCommentNotClosed = new(
        id: "SDA0503",
        title: "SQL comment is not closed",
        messageFormat: "A SQL comment is not closed. method=[{0}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlQuoteNotClosed = new(
        id: "SDA0504",
        title: "SQL quote is not closed",
        messageFormat: "A SQL string literal quote is not closed. method=[{0}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlUnknownPragma = new(
        id: "SDA0505",
        title: "Unknown SQL pragma",
        messageFormat: "Unknown SQL pragma '/*!{1} */'; expected '!helper' or '!using'. method=[{0}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlCodeBlockBraceUnclosed = new(
        id: "SDA0506",
        title: "SQL code block has an unclosed brace",
        messageFormat: "A /*% %/ code block opens a '{{' that is never closed; balance the braces across the code blocks. method=[{0}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlCodeBlockBraceExtraClose = new(
        id: "SDA0507",
        title: "SQL code block has an unmatched closing brace",
        messageFormat: "A /*% %/ code block has a '}}' with no matching '{{'; balance the braces across the code blocks. method=[{0}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UndefinedSqlParameter = new(
        id: "SDA0508",
        title: "SQL parameter does not match method parameters",
        messageFormat: "SQL parameter '@{1}' is not defined as a method parameter. method=[{0}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnusedMethodParameter = new(
        id: "SDA0509",
        title: "Method parameter is unused in SQL",
        messageFormat: "Method parameter is declared but never referenced in SQL. method=[{0}], parameter=[{1}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlPropertyNotFound = new(
        id: "SDA0510",
        title: "SQL property accessor does not match a property",
        messageFormat: "/*@ {1}.{2} */ references a property that is not declared on the parameter. method=[{0}], parameter=[{1}], property=[{2}], type=[{3}].",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
