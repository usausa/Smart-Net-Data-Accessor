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
    // Structural diagnostics (SDA0170-0174, spec §11.5).
    // ------------------------------------------------------------------

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

    public static readonly DiagnosticDescriptor DialectNotIDialect = new(
        id: "SDA0174",
        title: "[DataAccessor(Dialect = typeof(X))] X does not implement IDialect",
        messageFormat: "Class=[{0}]: Dialect target type '{1}' does not implement Smart.Data.Accessor.Dialect.IDialect (spec §11.5)",
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
}
