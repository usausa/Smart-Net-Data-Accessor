namespace Smart.Data.Accessor.Shared.Builders;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// QueryBuilder generator diagnostics. Shared across providers (linked source); each generator assembly
// IDs use the SDA1xxx band so the reporting generator is
// identifiable from the number (the core generator owns SDA0xxx). Ordered by the ClassScanner /
// MethodResolver / per-provider transform pipeline: container → attribute → table → columns → key → mapping.
internal static class BuilderDiagnostics
{
    public static DiagnosticDescriptor InvalidContainer { get; } = new(
        id: "SDA1001",
        title: "Invalid container class",
        messageFormat: "QueryBuilder attribute requires a partial class. class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor QueryBuilderDuplicated { get; } = new(
        id: "SDA1002",
        title: "Multiple QueryBuilder attributes",
        messageFormat: "Only one QueryBuilder attribute is allowed. method=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor MissingTable { get; } = new(
        id: "SDA1003",
        title: "Missing entity type or table name",
        messageFormat: "Entity type or table name is required. method=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Raised by the column-listing kinds (Select / SelectSingle / Update / Merge / Upsert) when no entity type is given.
    public static DiagnosticDescriptor SelectColumnsUnresolvable { get; } = new(
        id: "SDA1004",
        title: "Columns cannot be determined",
        messageFormat: "Entity type is required to determine columns. method=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // Raised by the keyed kinds that still work without a key (Delete / SelectSingle / MySQL Upsert) when the entity has no [Key].
    public static DiagnosticDescriptor NoKeyForBuilder { get; } = new(
        id: "SDA1005",
        title: "Entity has no [Key]",
        messageFormat: "Entity has no property marked [Key]. method=[{1}], type=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypeMapTypeHandlerConflict { get; } = new(
        id: "SDA1006",
        title: "[TypeMap] conflicts with [TypeHandler]",
        messageFormat: "DbType conflicts with [TypeHandler<>]. class=[{0}], type=[{1}], property=[{2}]",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor KeyRequiredForBuilder { get; } = new(
        id: "SDA1007",
        title: "Entity has no [Key] required by the statement",
        messageFormat: "Entity has no property marked [Key], which the statement requires. method=[{1}], type=[{0}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor UndefinedEnumValue { get; } = new(
        id: "SDA1008",
        title: "Undefined enum value",
        messageFormat: "Attribute argument is not a defined enum value, and is not used. attribute=[{0}], value=[{1}], method=[{2}], property=[{3}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);
}
