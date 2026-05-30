namespace Smart.Data.Accessor.Builders.Generator.Models;

using SourceGenerateHelper;

// v1 skeleton (spec.md §7.11.1). Filled in by Phase 5.x.
internal sealed record BuilderEntityModel(
    string FullyQualifiedName,
    EquatableArray<BuilderColumnModel> Columns);
