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
}
