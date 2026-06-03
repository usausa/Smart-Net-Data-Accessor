namespace Smart.Data.Accessor.Builders.Generator.Models;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

// spec §7.11 (P4): the equatable per-accessor model produced by the FAWMN transform on [DataAccessor].
// One per [DataAccessor] class per Builder generator; Methods holds only the methods carrying that
// generator's QueryBuilder-derived attributes (empty => the generator emits nothing for this class).
// Diagnostics are collected during the transform and replayed at the output stage (which cannot run
// symbol analysis).
internal sealed record BuilderClassModel(
    string Namespace,
    string ClassName,
    Accessibility Accessibility,
    EquatableArray<BuilderMethodModel> Methods,
    EquatableArray<BuilderDiagnosticData> Diagnostics);
