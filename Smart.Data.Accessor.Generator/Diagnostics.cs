namespace Smart.Data.Accessor.Generator;

using Microsoft.CodeAnalysis;

// Core DataAccessor generator diagnostics. IDs follow a phase-based banding aligned with the pipeline
// and with spec §11:
//   SDA00xx  class-level        (BuildClassResult: class structure + class attributes [Inject]/[Provider]/[ExecuteConfig])  → spec §11.1
//   SDA01xx  method structure   (method partial / command-source exclusivity A-group・B-group)                              → spec §11.2
//   SDA02xx  parameter/direction(parameter attributes, [Direction], [DirectSql] param)                                     → spec §11.3
//   SDA03xx  return/mapping     (return-type shapes, reader, converter, [TypeHandler])                                     → spec §11.4
//   SDA04xx  SQL-file resolution(CompleteModel: SQL-file conflicts)                                                        → spec §11.5
//   SDA05xx  2-way SQL parse    (BuildSqlEmitCode: tokenizer / pragma / parameter checks)                                  → spec §11.6
// Builder generator diagnostics use the SDA1xxx band (see BuilderDiagnostics, spec §11.7).
// Retired IDs (not re-used): SDA0141 ConverterTypeNotSupported, SDA0149 EnumUnderlyingMismatch,
// SDA0150/SDA0220-0223 user-declared Builder method validation, SDA0186/0187 /*!helper*//*!using*/ existence
// checks. History is tracked in AnalyzerReleases.
internal static class Diagnostics
{
    // ==================================================================
    // SDA00xx — class-level (spec §11.1)
    // ==================================================================

    public static readonly DiagnosticDescriptor InvalidClass = new(
        id: "SDA0001",
        title: "Invalid DataAccessor class",
        messageFormat: "DataAccessor class must be declared as partial, class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAccessorClassNested = new(
        id: "SDA0002",
        title: "[DataAccessor] class must not be nested",
        messageFormat: "Class=[{0}] is declared as a nested type; [DataAccessor] classes must be top-level (spec §11.1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAccessorClassGeneric = new(
        id: "SDA0003",
        title: "[DataAccessor] class must not be generic",
        messageFormat: "Class=[{0}] is declared as generic; [DataAccessor] classes must not have type parameters (spec §11.1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNameDuplicated = new(
        id: "SDA0010",
        title: "[Inject] Name is duplicated within the class",
        messageFormat: "Class=[{0}]: [Inject(...)] Name '{1}' is declared more than once (spec §1.4 F1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNameConflictsWithMember = new(
        id: "SDA0011",
        title: "[Inject] Name conflicts with another member or method parameter",
        messageFormat: "Class=[{0}]: [Inject] Name '{1}' conflicts with an existing field, property, or method parameter (spec §1.4 F1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectTypeNotResolvable = new(
        id: "SDA0012",
        title: "[Inject] Type may not resolve from IServiceProvider",
        messageFormat: "Class=[{0}]: [Inject(typeof({1}), \"{2}\")] declares a type that may not be resolvable from IServiceProvider; runtime will throw if unregistered (spec §1.4 F1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNotReferenced = new(
        id: "SDA0013",
        title: "[Inject] declaration is not referenced",
        messageFormat: "Class=[{0}]: [Inject] Name '{1}' is referenced neither in SQL (/*@ {1}.X */ etc.) nor in user code (spec §1.4 F1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProviderNameEmpty = new(
        id: "SDA0014",
        title: "[Provider] name is empty",
        messageFormat: "Class=[{0}]: [Provider(\"\")] has an empty name; IDbProviderSelector.GetProvider will receive an empty string (spec §1.4 F7)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProviderOnPatternAOnlyAccessor = new(
        id: "SDA0015",
        title: "[Provider] has no effect on Pattern A only accessor",
        messageFormat: "Class=[{0}]: [Provider(\"{1}\")] is set but the accessor has no Pattern B methods; the name will never be passed to IDbProviderSelector.GetProvider (spec §1.4 F7)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteConfigProfileInvalid = new(
        id: "SDA0016",
        title: "[ExecuteConfig] target is not an [AccessorProfile]",
        messageFormat: "[ExecuteConfig(typeof({1}))] referenced by class=[{0}] does not have [AccessorProfile]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProfileCircularReference = new(
        id: "SDA0017",
        title: "[AccessorProfile] class also has [ExecuteConfig]",
        messageFormat: "Profile class=[{0}] has [ExecuteConfig], creating a circular reference",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA01xx — method structure / command-source exclusivity (spec §11.2)
    // ==================================================================

    public static readonly DiagnosticDescriptor InvalidMethod = new(
        id: "SDA0101",
        title: "Invalid DataAccessor method",
        messageFormat: "DataAccessor method must be a partial declaration, method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor PartialMethodAlreadyImplemented = new(
        id: "SDA0102",
        title: "Partial method implementation already exists",
        messageFormat: "Method=[{0}]: a partial implementation is already present in source; Generator cannot emit a second implementation (spec §11.2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // spec §11.2: the execution-kind attributes (A-group) are mutually exclusive.
    public static readonly DiagnosticDescriptor ExecutionKindDuplicated = new(
        id: "SDA0103",
        title: "Multiple execution-kind attributes on the same method",
        messageFormat: "Method=[{0}]: more than one execution attribute ([Execute]/[ExecuteScalar]/[Query]/[QueryFirst]/[ExecuteReader]) is present; they are mutually exclusive (spec §11.2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // spec §11.2: [Procedure] and [DirectSql] (B-group command sources) are mutually exclusive. The
    // QueryBuilder combinations are SDA0105 (core) / SDA1002 (builder); this fills the remaining gap.
    public static readonly DiagnosticDescriptor ProcedureDirectSqlConflict = new(
        id: "SDA0104",
        title: "[Procedure] combined with [DirectSql]",
        messageFormat: "Method=[{0}]: [Procedure] cannot be combined with [DirectSql]; the command source is ambiguous (spec §11.2)",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderAndCommandSourceConflict = new(
        id: "SDA0105",
        title: "QueryBuilder attribute combined with [Procedure] / [DirectSql]",
        messageFormat: "Method=[{0}]: a QueryBuilder attribute ([Insert]/[Update]/…) cannot be combined with [Procedure] or [DirectSql]; the SQL source is ambiguous (spec §11.2)",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodNameDuplicated = new(
        id: "SDA0106",
        title: "[MethodName] is duplicated within the class",
        messageFormat: "Class=[{0}]: [MethodName(\"{1}\")] is declared on multiple methods, which would collide in SQL-file lookup (spec §1.4 F11)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA02xx — parameter / direction (spec §11.3)
    // ==================================================================

    public static readonly DiagnosticDescriptor NameDuplicated = new(
        id: "SDA0201",
        title: "Duplicate [Name] on parameters or properties",
        messageFormat: "Method=[{0}]: multiple parameters or properties share [Name(\"{1}\")] (spec §11.3)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlInjectionWarning = new(
        id: "SDA0202",
        title: "[DirectSql] passes raw SQL (SQL injection responsibility is the caller's)",
        messageFormat: "Method=[{0}]: [DirectSql] binds the first parameter directly to cmd.CommandText, so preventing SQL injection is the caller's responsibility; set [DirectSql(SuppressWarning = true)] to silence (spec §5.2 / §1.4 F6)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlFirstParamNotString = new(
        id: "SDA0203",
        title: "[DirectSql] method first parameter must be string",
        messageFormat: "Method=[{0}]: [DirectSql] requires the first parameter (after conn/tx/CancellationToken) to be `string` for the command text (spec §5.2 / §1.4 F6)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProcedureNameEmpty = new(
        id: "SDA0204",
        title: "[Procedure] stored procedure name is empty",
        messageFormat: "Method=[{0}]: [Procedure(\"\")] has an empty stored procedure name (spec §1.4 F2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AsyncProcedureRefParam = new(
        id: "SDA0205",
        title: "async [Procedure] cannot use out/ref parameters",
        messageFormat: "Method=[{0}]: async [Procedure] cannot use out/ref parameter '{1}'; switch to a synchronous method or POCO aggregation (spec §1.4 F2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DbTypeAttributeConflict = new(
        id: "SDA0206",
        title: "Conflicting [DbType] / [DbType<TEnum>] on same parameter",
        messageFormat: "Method=[{0}]: parameter '{1}' has both [DbType(DbType)] and [DbType<TEnum>] (spec §1.4 F15)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DbTypeProviderEnumNotWhitelisted = new(
        id: "SDA0207",
        title: "[DbType<TEnum>] TEnum is not in the provider enum whitelist",
        messageFormat: "Method=[{0}]: parameter '{1}' uses [DbType<{2}>] where TEnum is not in the spec §5.3.1 whitelist; the provider-specific DbType assignment will be skipped",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectionRefKindMismatch = new(
        id: "SDA0208",
        title: "[Direction] conflicts with the parameter modifier",
        messageFormat: "Method=[{0}]: parameter '{1}' has [Direction({2})] but parameter modifier is '{3}' (spec §1.4 F4)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectionOnUnsupportedMethod = new(
        id: "SDA0209",
        title: "[Direction] used on unsupported method kind",
        messageFormat: "Method=[{0}]: parameter '{1}' has [Direction] but the method is not [Procedure] / [Execute] / [DirectSql] (spec §1.4 F4 / F14)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ReturnValueDirectionNotAllowed = new(
        id: "SDA0210",
        title: "[Direction(ReturnValue)] is not supported",
        messageFormat: "Method=[{0}]: parameter '{1}' cannot use [Direction(ReturnValue)]; the stored procedure RETURN value maps to the method's scalar return value (spec §5.6)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlCommandTextDirection = new(
        id: "SDA0211",
        title: "[Direction] not allowed on [DirectSql] command-text parameter",
        messageFormat: "Method=[{0}]: command-text string parameter '{1}' on a [DirectSql] method cannot be annotated with [Direction] (spec §1.4 F14)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA03xx — return shape / mapping / converter (spec §11.4)
    //
    // Mapping / converter validation (spec §7.4 / §7.10) is wired by ConverterResolver, invoked from
    // AccessorModelBuilder.BuildColumnInfos. Spike result (spec §15 #7): Roslyn surfaces `static abstract`
    // interface members as IsAbstract && IsStatic; SDA0310 instead checks that the converter exposes
    // accessible static FromDb/ToDb (implicit interface implementation) so `TConverter.FromDb(...)` binds.
    // ==================================================================

    public static readonly DiagnosticDescriptor UnsupportedReturn = new(
        id: "SDA0301",
        title: "Unsupported return type",
        messageFormat: "Return type is not supported in prototype, method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReturnInvalid = new(
        id: "SDA0302",
        title: "[Execute] return type must be int/void/Task<int>/Task",
        messageFormat: "Method=[{0}]: [Execute] return type '{1}' is not one of int/void/Task<int>/Task/ValueTask<int>/ValueTask (spec §11.4)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReaderInvalidReturn = new(
        id: "SDA0303",
        title: "[ExecuteReader] return type is not a reader",
        messageFormat: "Method=[{0}]: [ExecuteReader] return type '{1}' is not IDataReader / DbDataReader / Task<...> / ValueTask<...> (spec §1.4 F3)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReaderRequiresUsing = new(
        id: "SDA0304",
        title: "[ExecuteReader] result must be disposed by the caller",
        messageFormat: "Method=[{0}]: [ExecuteReader] returns a reader that owns its command (and connection for Pattern B); callers must dispose it with `using` (spec §1.4 F3 / §4.1.1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AsyncEnumerableMissingEnumeratorCancellation = new(
        id: "SDA0305",
        title: "IAsyncEnumerable method requires [EnumeratorCancellation] CancellationToken",
        messageFormat: "IAsyncEnumerable<T> method '{0}' should declare a CancellationToken parameter annotated with [EnumeratorCancellation]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor RecordPrimaryConstructorPath = new(
        id: "SDA0306",
        title: "Record entity is mapped via primary constructor",
        messageFormat: "Method=[{0}]: entity record '{1}' is mapped via its primary constructor (positional binding, spec §7.8 / §7.10.5)",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NonNullableDbNull = new(
        id: "SDA0307",
        title: "Non-nullable property may receive DB NULL",
        messageFormat: "Property '{1}' on method=[{0}] is a non-nullable reference type; DB NULL will fall through as default! (null) (spec §7.10.1)",
        category: "Mapping",
        // Info: advisory only. As a warning it would fire on nearly every non-nullable
        // reference-type column and conflict with the project's zero-warning policy.
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterTClrMismatch = new(
        id: "SDA0308",
        title: "Converter TClr does not match property type",
        messageFormat: "Converter on property '{1}' (method=[{0}]) declares TClr that does not match the property type",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterNotIValueConverter = new(
        id: "SDA0309",
        title: "Converter type does not implement IValueConverter<,>",
        messageFormat: "Converter type '{1}' referenced on method=[{0}] does not implement IValueConverter<TDb, TClr>",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterStaticAbstractMissing = new(
        id: "SDA0310",
        title: "Converter type missing static abstract implementation",
        messageFormat: "Converter type '{1}' on method=[{0}] does not provide a static implementation of FromDb/ToDb",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeHandlerDuplicated = new(
        id: "SDA0311",
        title: "Multiple [TypeHandler] on same property",
        messageFormat: "Property '{1}' on method=[{0}] has multiple [TypeHandler<>] attributes; only one converter is honored",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA04xx — SQL-file resolution (spec §11.5)
    // ==================================================================

    public static readonly DiagnosticDescriptor SqlNotFound = new(
        id: "SDA0401",
        title: "SQL file not found",
        messageFormat: "Neither SQL file nor Builder specified for method=[{0}], expected additional file '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlFileNameCollision = new(
        id: "SDA0402",
        title: "SQL file name collision",
        messageFormat: "Method=[{0}]: multiple SQL files resolve to the same logical name '{1}' (spec §11.5)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlHasSqlFile = new(
        id: "SDA0403",
        title: "[DirectSql] method must not have a corresponding SQL file",
        messageFormat: "Method=[{0}]: [DirectSql] method must not have a corresponding SQL file '{1}' (spec §5.2 / §1.4 F6)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProcedureHasSqlFile = new(
        id: "SDA0404",
        title: "[Procedure] method must not have a corresponding SQL file",
        messageFormat: "Method=[{0}]: [Procedure] is set but a corresponding SQL file '{1}' also exists (spec §1.4 F2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderAndSqlBothPresent = new(
        id: "SDA0405",
        title: "Both SQL file and a QueryBuilder attribute are present",
        messageFormat: "Method=[{0}]: both a SQL file '{1}' and a QueryBuilder attribute ([Insert]/[Update]/…) are present; resolution is ambiguous (spec §11.5)",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ==================================================================
    // SDA05xx — 2-way SQL parse (spec §11.6)
    // ==================================================================

    public static readonly DiagnosticDescriptor SqlTokenizeFailed = new(
        id: "SDA0501",
        title: "Failed to tokenize SQL",
        messageFormat: "Failed to tokenize SQL for method=[{0}]: {1}",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlEmpty = new(
        id: "SDA0502",
        title: "SQL is empty",
        messageFormat: "SQL is empty for method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlCommentNotClosed = new(
        id: "SDA0503",
        title: "SQL comment is not closed",
        messageFormat: "Method=[{0}]: a SQL comment is not closed (spec §11.6)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlQuoteNotClosed = new(
        id: "SDA0504",
        title: "SQL quote is not closed",
        messageFormat: "Method=[{0}]: a SQL string literal quote is not closed (spec §11.6)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlUnknownPragma = new(
        id: "SDA0505",
        title: "Unknown SQL pragma",
        messageFormat: "Method=[{0}]: unknown SQL pragma '/*!{1} */'; expected '!helper' or '!using' (spec §11.6)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlCodeBlockBraceUnclosed = new(
        id: "SDA0506",
        title: "SQL code block has an unclosed brace",
        messageFormat: "Method=[{0}]: a /*% %/ code block opens a '{{' that is never closed; balance the braces across the code blocks (spec §11.6)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlCodeBlockBraceExtraClose = new(
        id: "SDA0507",
        title: "SQL code block has an unmatched closing brace",
        messageFormat: "Method=[{0}]: a /*% %/ code block has a '}}' with no matching '{{'; balance the braces across the code blocks (spec §11.6)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UndefinedSqlParameter = new(
        id: "SDA0508",
        title: "SQL parameter does not match method parameters",
        messageFormat: "SQL parameter '@{1}' is not defined as a method parameter on method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnusedMethodParameter = new(
        id: "SDA0509",
        title: "Method parameter is unused in SQL",
        messageFormat: "Method parameter '{1}' is declared on method=[{0}] but never referenced in SQL",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlPropertyNotFound = new(
        id: "SDA0510",
        title: "SQL property accessor does not match a property",
        messageFormat: "Method=[{0}]: /*@ {1}.{2} */ references property '{2}' which is not declared on parameter '{1}' (type '{3}')",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
