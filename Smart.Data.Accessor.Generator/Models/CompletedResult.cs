namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// spec §7.11 (P3): equatable result after the SQL stage (Combine with .sql files). The model has its
// SQL fields filled / error methods dropped; Diagnostics include the symbol + SQL stages.
internal sealed record CompletedResult(
    AccessorModel? Model,
    EquatableArray<DiagnosticData> Diagnostics);
