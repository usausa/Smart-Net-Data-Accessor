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

    // ------------------------------------------------------------------
    // Mapping / Builder diagnostics (SDA0130 series).
    // ------------------------------------------------------------------

    public static readonly DiagnosticDescriptor NoKeyForBuilder = new(
        id: "SDA0130",
        title: "Entity has no [Key] for Update/Delete builder",
        messageFormat: "Entity '{0}' has no property marked [Key]; Update/Delete builder cannot emit a WHERE clause for method=[{1}]",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor BuilderRequiresEntityParameter = new(
        id: "SDA0131",
        title: "Builder method requires entity parameter",
        messageFormat: "Update builder method=[{0}] requires an entity parameter after `ref BuilderContext`",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
