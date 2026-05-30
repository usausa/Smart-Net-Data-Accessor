namespace Smart.Data.Accessor.Builders.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static readonly DiagnosticDescriptor InvalidMethod = new(
        id: "SDB0001",
        title: "Invalid builder method",
        messageFormat: "InsertBuilder method must be a partial declaration, method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidContainer = new(
        id: "SDB0002",
        title: "Invalid container class",
        messageFormat: "InsertBuilder method must be in a partial class, class=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor InvalidSignature = new(
        id: "SDB0003",
        title: "Invalid builder signature",
        messageFormat: "InsertBuilder method must accept (BuilderContext, TEntity), method=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

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
        messageFormat: "Builder method=[{0}] requires an entity parameter after `ref BuilderContext`",
        category: "Builder",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeMapTypeHandlerConflict = new(
        id: "SDA0148",
        title: "[TypeMap] DbType conflicts with [TypeHandler]",
        messageFormat: "[TypeMap] for type '{1}' on class=[{0}] declares a DbType that conflicts with [TypeHandler<>] on property '{2}'; [TypeHandler] takes precedence",
        category: "Mapping",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
