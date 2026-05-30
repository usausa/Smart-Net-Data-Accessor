namespace Smart.Data.Accessor.Generator.Models;

using SourceGenerateHelper;

// v1 skeleton (spec.md §7.11.1). Filled in by Phase 6.3.
internal sealed record EntityModel(
    string FullyQualifiedName,
    EntityCtorKind CtorKind,
    EquatableArray<ColumnModel> Columns);

internal enum EntityCtorKind
{
    Parameterless,
    PrimaryConstructor
}
