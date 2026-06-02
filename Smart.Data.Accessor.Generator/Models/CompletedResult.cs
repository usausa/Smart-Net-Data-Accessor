namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// spec §7.11 (P3): equatable result after the SQL stage (Combine with .sql files). The model has its
// SQL fields filled / error methods dropped; Diagnostics include symbol + SQL stage; Usings are the
// /*!using*/ / /*!helper*/ directives to validate against the Compilation (separate diagnostic branch).
internal sealed record CompletedResult(
    AccessorModel? Model,
    EquatableArray<DiagnosticData> Diagnostics,
    EquatableArray<UsingValidation> Usings);
