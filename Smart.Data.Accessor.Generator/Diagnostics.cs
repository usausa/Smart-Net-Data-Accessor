namespace Smart.Data.Accessor.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor InvalidClass = new(
        id: "SDA0001",
        title: "Invalid DataAccessor class",
        messageFormat: "DataAccessor class must be declared as partial, class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidMethod = new(
        id: "SDA0002",
        title: "Invalid DataAccessor method",
        messageFormat: "DataAccessor method must be a partial declaration, method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlNotFound = new(
        id: "SDA0003",
        title: "SQL file not found",
        messageFormat: "Neither SQL file nor Builder specified for method=[{0}], expected additional file '{1}'",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnsupportedReturn = new(
        id: "SDA0004",
        title: "Unsupported return type",
        messageFormat: "Return type is not supported in prototype, method=[{0}], type=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // 2-way SQL diagnostics (SDA0100 series).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor SqlTokenizeFailed = new(
        id: "SDA0100",
        title: "Failed to tokenize SQL",
        messageFormat: "Failed to tokenize SQL for method=[{0}]: {1}",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlEmpty = new(
        id: "SDA0101",
        title: "SQL is empty",
        messageFormat: "SQL is empty for method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlCommentNotClosed = new(
        id: "SDA0102",
        title: "SQL comment is not closed",
        messageFormat: "Method=[{0}]: a SQL comment is not closed (spec §11.2)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlQuoteNotClosed = new(
        id: "SDA0103",
        title: "SQL quote is not closed",
        messageFormat: "Method=[{0}]: a SQL string literal quote is not closed (spec §11.2)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlUnknownPragma = new(
        id: "SDA0104",
        title: "Unknown SQL pragma",
        messageFormat: "Method=[{0}]: unknown SQL pragma '/*!{1} */'; expected '!helper' or '!using' (spec §11.2)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UndefinedSqlParameter = new(
        id: "SDA0110",
        title: "SQL parameter does not match method parameters",
        messageFormat: "SQL parameter '@{1}' is not defined as a method parameter on method=[{0}]",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UnusedMethodParameter = new(
        id: "SDA0111",
        title: "Method parameter is unused in SQL",
        messageFormat: "Method parameter '{1}' is declared on method=[{0}] but never referenced in SQL",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlPropertyNotFound = new(
        id: "SDA0112",
        title: "SQL property accessor does not match a property",
        messageFormat: "Method=[{0}]: /*@ {1}.{2} */ references property '{2}' which is not declared on parameter '{1}' (type '{3}')",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // SDA0130 (NoKeyForBuilder) / SDA0131 (BuilderRequiresEntityParameter) are
    // reported by Smart.Data.Accessor.Builders.Generator and live there.

    public static readonly DiagnosticDescriptor RecordPrimaryConstructorPath = new(
        id: "SDA0133",
        title: "Record entity is mapped via primary constructor",
        messageFormat: "Method=[{0}]: entity record '{1}' is mapped via its primary constructor (positional binding, spec §7.8 / §7.10.5)",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // Mapping diagnostics (SDA0140-0149).
    //
    // Spike result (spec §15 #7):
    //   Roslyn surfaces `static abstract` interface members as
    //   `IMethodSymbol.IsAbstract == true && IsStatic == true`.
    //   Implementations are discovered by walking the implementing type's
    //   members and matching by signature. This is the SDA0144 prerequisite.
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor NonNullableDbNull = new(
        id: "SDA0140",
        title: "Non-nullable property may receive DB NULL",
        messageFormat: "Property '{1}' on method=[{0}] is non-nullable; DB NULL will fall through as default! (spec §7.10.1)",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterTypeNotSupported = new(
        id: "SDA0141",
        title: "Property type not in [ConverterSupportedTypes]",
        messageFormat: "Property '{1}' on method=[{0}] has a type that is not allowed by the converter's [ConverterSupportedTypes] whitelist",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterTClrMismatch = new(
        id: "SDA0142",
        title: "Converter TClr does not match property type",
        messageFormat: "Converter on property '{1}' (method=[{0}]) declares TClr that does not match the property type",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterNotIValueConverter = new(
        id: "SDA0143",
        title: "Converter type does not implement IValueConverter<,>",
        messageFormat: "Converter type '{1}' referenced on method=[{0}] does not implement IValueConverter<TDb, TClr>",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConverterStaticAbstractMissing = new(
        id: "SDA0144",
        title: "Converter type missing static abstract implementation",
        messageFormat: "Converter type '{1}' on method=[{0}] does not provide a static implementation of FromDb/ToDb",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeHandlerDuplicated = new(
        id: "SDA0145",
        title: "Multiple [TypeHandler] on same property",
        messageFormat: "Property '{1}' on method=[{0}] has multiple [TypeHandler<>] attributes; only one converter is honored",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteConfigProfileInvalid = new(
        id: "SDA0146",
        title: "[ExecuteConfig] target is not an [AccessorProfile]",
        messageFormat: "[ExecuteConfig(typeof({1}))] referenced by class=[{0}] does not have [AccessorProfile]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProfileCircularReference = new(
        id: "SDA0147",
        title: "[AccessorProfile] class also has [ExecuteConfig]",
        messageFormat: "Profile class=[{0}] has [ExecuteConfig], creating a circular reference",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMapTypeHandlerConflict = new(
        id: "SDA0148",
        title: "[TypeMap] DbType conflicts with [TypeHandler]",
        messageFormat: "[TypeMap] for type '{1}' on class=[{0}] declares a DbType that conflicts with the [TypeHandler<>] inferred from TDb; [TypeHandler] takes precedence",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor EnumUnderlyingMismatch = new(
        id: "SDA0149",
        title: "Enum underlying type may not match DB column type",
        messageFormat: "Property '{1}' on method=[{0}] is an enum whose underlying type may not match the DB column",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // [Execute(Builder = ...)] pass-through diagnostics (SDA0150 / SDA0220-0223).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor BuilderMethodNotFound = new(
        id: "SDA0150",
        title: "Builder method not found",
        messageFormat: "Builder method '{1}' referenced by [Execute(Builder = nameof({1}))] / [Query(Builder = nameof({1}))] was not found on method=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderFirstArgInvalid = new(
        id: "SDA0220",
        title: "Builder first argument must be ref BuilderContext",
        messageFormat: "Builder method '{1}' referenced by method=[{0}] must declare its first parameter as 'ref BuilderContext'",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderArgMismatch = new(
        id: "SDA0221",
        title: "Builder argument list does not match Execute method",
        messageFormat: "Builder method '{1}' arguments (from index 1) do not match Execute method=[{0}] parameter list (type, order, or name)",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderReturnInvalid = new(
        id: "SDA0222",
        title: "Builder return type must be void",
        messageFormat: "Builder method '{1}' referenced by method=[{0}] must return void",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderIsAsync = new(
        id: "SDA0223",
        title: "Builder method must not be async",
        messageFormat: "Builder method '{1}' referenced by method=[{0}] must not be declared async (ref struct cannot live across await)",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // Iterator / async-iterator diagnostics (SDA0198 series).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor AsyncEnumerableMissingEnumeratorCancellation = new(
        id: "SDA0198",
        title: "IAsyncEnumerable method requires [EnumeratorCancellation] CancellationToken",
        messageFormat: "IAsyncEnumerable<T> method '{0}' should declare a CancellationToken parameter annotated with [EnumeratorCancellation]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // Builder diagnostics (SDA0152, spec §11.4).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor BuilderAndSqlBothPresent = new(
        id: "SDA0152",
        title: "Both SQL file and Builder are present",
        messageFormat: "Method=[{0}]: both a SQL file '{1}' and a Builder reference are present; resolution is ambiguous (spec §11.4)",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // Structural diagnostics (SDA0170-0174, SDA0172, spec §11.5).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor PartialMethodAlreadyImplemented = new(
        id: "SDA0172",
        title: "Partial method implementation already exists",
        messageFormat: "Method=[{0}]: a partial implementation is already present in source; Generator cannot emit a second implementation (spec §11.5)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAccessorClassNested = new(
        id: "SDA0170",
        title: "[DataAccessor] class must not be nested",
        messageFormat: "Class=[{0}] is declared as a nested type; [DataAccessor] classes must be top-level (spec §11.5)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DataAccessorClassGeneric = new(
        id: "SDA0171",
        title: "[DataAccessor] class must not be generic",
        messageFormat: "Class=[{0}] is declared as generic; [DataAccessor] classes must not have type parameters (spec §11.5)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SqlFileNameCollision = new(
        id: "SDA0173",
        title: "SQL file name collision",
        messageFormat: "Method=[{0}]: multiple SQL files resolve to the same logical name '{1}' (spec §11.5)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // Empty-string attribute warnings (SDA0183 / SDA0192, spec §1.4 F2 / F7 / §11.5).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor ProviderNameEmpty = new(
        id: "SDA0183",
        title: "[Provider] name is empty",
        messageFormat: "Class=[{0}]: [Provider(\"\")] has an empty name; IConnectionFactory.Create(name) will receive an empty string (spec §1.4 F7)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProcedureNameEmpty = new(
        id: "SDA0192",
        title: "[Procedure] stored procedure name is empty",
        messageFormat: "Method=[{0}]: [Procedure(\"\")] has an empty stored procedure name (spec §1.4 F2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // [Inject] / [MethodName] duplicate-name diagnostics (SDA0180, SDA0185, SDA0188, spec §1.4 F1 / F11 / §11.5).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor InjectNameDuplicated = new(
        id: "SDA0180",
        title: "[Inject] Name is duplicated within the class",
        messageFormat: "Class=[{0}]: [Inject(...)] Name '{1}' is declared more than once (spec §1.4 F1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MethodNameDuplicated = new(
        id: "SDA0185",
        title: "[MethodName] is duplicated within the class",
        messageFormat: "Class=[{0}]: [MethodName(\"{1}\")] is declared on multiple methods, which would collide in SQL-file lookup (spec §1.4 F11)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InjectNameConflictsWithMember = new(
        id: "SDA0188",
        title: "[Inject] Name conflicts with another member or method parameter",
        messageFormat: "Class=[{0}]: [Inject] Name '{1}' conflicts with an existing field, property, or method parameter (spec §1.4 F1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // [Inject] diagnostics (SDA0181, spec §1.4 F1 / §11.5).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor InjectTypeNotResolvable = new(
        id: "SDA0181",
        title: "[Inject] Type may not resolve from IServiceProvider",
        messageFormat: "Class=[{0}]: [Inject(typeof({1}), \"{2}\")] declares a type that may not be resolvable from IServiceProvider; runtime will throw if unregistered (spec §1.4 F1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // [Execute] / [Name] diagnostics (SDA0132, SDA0134, spec §11.3.1).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor NameDuplicated = new(
        id: "SDA0132",
        title: "Duplicate [Name] on parameters or properties",
        messageFormat: "Method=[{0}]: multiple parameters or properties share [Name(\"{1}\")] (spec §11.3.1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReturnInvalid = new(
        id: "SDA0134",
        title: "[Execute] return type must be int/void/Task<int>/Task",
        messageFormat: "Method=[{0}]: [Execute] return type '{1}' is not one of int/void/Task<int>/Task/ValueTask<int>/ValueTask (spec §11.3.1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // F12: /*!helper */ / /*!using */ pragmas (SDA0186-0187, spec §1.4 F12 / §6.3).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor HelperTypeNotFound = new(
        id: "SDA0186",
        title: "/*!helper */ type cannot be resolved",
        messageFormat: "Method=[{0}]: /*!helper {1} */ references a type that cannot be resolved in the current Compilation (spec §1.4 F12)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor UsingNamespaceNotFound = new(
        id: "SDA0187",
        title: "/*!using */ namespace cannot be resolved",
        messageFormat: "Method=[{0}]: /*!using {1} */ references a namespace that cannot be resolved in the current Compilation (spec §1.4 F12)",
        category: "Sql",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // [DirectSql] structural diagnostics (SDA0128 / SDA0129, spec §5.2 / §1.4 F6).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor DirectSqlFirstParamNotString = new(
        id: "SDA0128",
        title: "[DirectSql] method first parameter must be string",
        messageFormat: "Method=[{0}]: [DirectSql] requires the first parameter (after conn/tx/CancellationToken) to be `string` for the command text (spec §5.2 / §1.4 F6)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlHasSqlFile = new(
        id: "SDA0129",
        title: "[DirectSql] method must not have a corresponding SQL file",
        messageFormat: "Method=[{0}]: [DirectSql] method must not have a corresponding SQL file '{1}' (spec §5.2 / §1.4 F6)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // X1: [DirectSql] + [Direction] interop diagnostics (SDA0200-0201, spec §1.4 F14 / §5.2 / §5.3).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor DirectSqlReturnValueDirection = new(
        id: "SDA0200",
        title: "[Direction(ReturnValue)] not allowed on [DirectSql] parameter",
        messageFormat: "Method=[{0}]: parameter '{1}' on a [DirectSql] method cannot use [Direction(ReturnValue)] (spec §1.4 F14)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectSqlCommandTextDirection = new(
        id: "SDA0201",
        title: "[Direction] not allowed on [DirectSql] command-text parameter",
        messageFormat: "Method=[{0}]: command-text string parameter '{1}' on a [DirectSql] method cannot be annotated with [Direction] (spec §1.4 F14)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // X2: DbType<TEnum> diagnostics (SDA0203-0204, spec §1.4 F15 / §5.3 / §5.3.1).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor DbTypeAttributeConflict = new(
        id: "SDA0203",
        title: "Conflicting [DbType] / [DbType<TEnum>] on same parameter",
        messageFormat: "Method=[{0}]: parameter '{1}' has both [DbType(DbType)] and [DbType<TEnum>] (spec §1.4 F15)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DbTypeProviderEnumNotWhitelisted = new(
        id: "SDA0204",
        title: "[DbType<TEnum>] TEnum is not in the provider enum whitelist",
        messageFormat: "Method=[{0}]: parameter '{1}' uses [DbType<{2}>] where TEnum is not in the spec §5.3.1 whitelist; the provider-specific DbType assignment will be skipped",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // [Provider] additional diagnostics (SDA0184, spec §1.4 F7 / §11.5).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor ProviderOnPatternAOnlyAccessor = new(
        id: "SDA0184",
        title: "[Provider] has no effect on Pattern A only accessor",
        messageFormat: "Class=[{0}]: [Provider(\"{1}\")] is set but the accessor has no Pattern B methods; the name will never be passed to IConnectionFactory.Create (spec §1.4 F7)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    // ------------------------------------------------------------------
    // [Procedure] / [ExecuteReader] / [Direction] diagnostics (SDA0190-0197, spec §1.4 F2 / F3 / F4 / §11.3.2).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor ProcedureHasSqlFile = new(
        id: "SDA0190",
        title: "[Procedure] method must not have a corresponding SQL file",
        messageFormat: "Method=[{0}]: [Procedure] is set but a corresponding SQL file '{1}' also exists (spec §1.4 F2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AsyncProcedureRefParam = new(
        id: "SDA0191",
        title: "async [Procedure] cannot use out/ref parameters",
        messageFormat: "Method=[{0}]: async [Procedure] cannot use out/ref parameter '{1}'; switch to a synchronous method or POCO aggregation (spec §1.4 F2)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReaderInvalidReturn = new(
        id: "SDA0193",
        title: "[ExecuteReader] return type is not a reader",
        messageFormat: "Method=[{0}]: [ExecuteReader] return type '{1}' is not IDataReader / DbDataReader / Task<...> / ValueTask<...> (spec §1.4 F3)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ExecuteReaderRequiresUsing = new(
        id: "SDA0194",
        title: "[ExecuteReader] result must be disposed by the caller",
        messageFormat: "Method=[{0}]: [ExecuteReader] returns a reader that owns its command (and connection for Pattern B); callers must dispose it with `using` (spec §1.4 F3 / §4.1.1)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectionRefKindMismatch = new(
        id: "SDA0195",
        title: "[Direction] conflicts with the parameter modifier",
        messageFormat: "Method=[{0}]: parameter '{1}' has [Direction({2})] but parameter modifier is '{3}' (spec §1.4 F4)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor DirectionOnUnsupportedMethod = new(
        id: "SDA0197",
        title: "[Direction] used on unsupported method kind",
        messageFormat: "Method=[{0}]: parameter '{1}' has [Direction] but the method is not [Procedure] / [Execute] / [DirectSql] (spec §1.4 F4 / F14)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
