namespace Smart.Data.Accessor.Generator.Models;

// v1 skeleton (spec.md §7.11.1). Filled in by Phase 6.3.
internal sealed record ConverterBindingModel(
    string ConverterTypeFullName,
    string DbClrTypeFullName,
    string ClrTypeFullName);
