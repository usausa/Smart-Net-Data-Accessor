namespace Smart.Data.Accessor.Builders.Generator.Engine;

using Microsoft.CodeAnalysis;

// QueryBuilder generator diagnostics. Shared across providers (linked source); each generator assembly
// tracks them in its AnalyzerReleases. IDs use the SDA1xxx band so the reporting generator is
// identifiable from the number (the core generator owns SDA0xxx). Ordered by the BuilderModelBuilder
// pipeline: container → attribute → table → columns → key → mapping.
internal static class BuilderDiagnostics
{
    public static readonly DiagnosticDescriptor InvalidContainer = new(
        id: "SDA1001",
        title: "Invalid container class",
        messageFormat: "A QueryBuilder attribute ([Insert]/[Update]/…) must be on a method in a partial class, class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor QueryBuilderDuplicated = new(
        id: "SDA1002",
        title: "Multiple QueryBuilder attributes on one method",
        messageFormat: "Method=[{0}]: more than one QueryBuilder attribute ([Insert]/[Update]/…) is present; only one is allowed",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MissingTable = new(
        id: "SDA1003",
        title: "QueryBuilder attribute needs an entity type or a table name",
        messageFormat: "Method=[{0}]: the QueryBuilder attribute specifies neither an entity type ([Insert(typeof(T))]) nor a table name ([Insert(Table = \"...\")])",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor SelectColumnsUnresolvable = new(
        id: "SDA1004",
        title: "Select/SelectSingle columns cannot be determined",
        messageFormat: "Method=[{0}]: [Select]/[SelectSingle] needs an entity type to determine the column list; specify [Select(typeof(T))]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor NoKeyForBuilder = new(
        id: "SDA1005",
        title: "Entity has no [Key] for Update/Delete/SelectSingle builder",
        messageFormat: "Entity '{0}' has no property marked [Key]; Update/Delete/SelectSingle builder cannot emit a WHERE clause for method=[{1}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMapTypeHandlerConflict = new(
        id: "SDA1006",
        title: "[TypeMap] DbType conflicts with [TypeHandler]",
        messageFormat: "[TypeMap] for type '{1}' on class=[{0}] declares a DbType that conflicts with [TypeHandler<>] on property '{2}'; [TypeHandler] takes precedence",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
