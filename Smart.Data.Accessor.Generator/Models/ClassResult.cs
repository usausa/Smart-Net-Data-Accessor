namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// spec §7.11 (P3): equatable result of the FAWMN transform (symbol stage). Carries the symbol-built
// model (null on class-level error) plus the symbol-stage diagnostics. Flows through the pipeline.
internal sealed record ClassResult(
    AccessorModel? Model,
    EquatableArray<DiagnosticData> Diagnostics);
