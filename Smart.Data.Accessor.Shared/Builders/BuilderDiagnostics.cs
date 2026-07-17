namespace Smart.Data.Accessor.Shared.Builders;

using Microsoft.CodeAnalysis;

// QueryBuilder generator diagnostics. Shared across providers (linked source); each generator assembly
// IDs use the SDA1xxx band so the reporting generator is
// identifiable from the number (the core generator owns SDA0xxx). Ordered by the ClassScanner /
// MethodResolver / per-provider transform pipeline: container → attribute → table → columns → key → mapping.
internal static class BuilderDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidContainer = new(
        id: "SDA1001",
        title: "Invalid container class",
        messageFormat: "A QueryBuilder attribute ([Insert]/[Update]/…) must be on a method in a partial class. class=[{0}].",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryBuilderDuplicated = new(
        id: "SDA1002",
        title: "Multiple QueryBuilder attributes on one method",
        messageFormat: "More than one QueryBuilder attribute ([Insert]/[Update]/…) is present; only one is allowed. method=[{0}].",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingTable = new(
        id: "SDA1003",
        title: "QueryBuilder attribute needs an entity type or a table name",
        messageFormat: "The QueryBuilder attribute specifies neither an entity type ([Insert(typeof(T))]) nor a table name ([Insert(Table = \"...\")]). method=[{0}].",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Raised by the column-listing kinds (Select / SelectSingle / Update / Merge / Upsert) when no entity type is given.
    public static readonly DiagnosticDescriptor SelectColumnsUnresolvable = new(
        id: "SDA1004",
        title: "Builder columns cannot be determined without an entity type",
        messageFormat: "This QueryBuilder needs an entity type (typeof(T)) to determine the column list. method=[{0}].",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    // Raised by the keyed kinds (Update / Delete / SelectSingle / Merge / Upsert) when the entity has no [Key].
    public static readonly DiagnosticDescriptor NoKeyForBuilder = new(
        id: "SDA1005",
        title: "Entity has no [Key] for the builder's WHERE/ON clause",
        messageFormat: "The entity has no property marked [Key]; the QueryBuilder cannot build its WHERE/ON clause. method=[{1}], type=[{0}].",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMapTypeHandlerConflict = new(
        id: "SDA1006",
        title: "[TypeMap] DbType conflicts with [TypeHandler]",
        messageFormat: "[TypeMap] declares a DbType that conflicts with [TypeHandler<>] on the property; [TypeHandler] takes precedence. class=[{0}], type=[{1}], property=[{2}].",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
