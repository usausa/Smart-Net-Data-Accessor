// The per-provider QueryBuilder generators (SqlServer / MySql / Postgres) deliberately declare
// provider namespaces — `Smart.Data.Accessor.Builders.<Provider>.Generator` — because that fully
// qualified name is a public contract: the test harness and consumers instantiate the generators by
// it. The three providers are co-located in this single assembly under `Providers/` (Phase 7
// consolidation, spec §8.1), so the folder cannot match the namespace and IDE0130 is unavoidable.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "Provider generator namespace is a public FQN contract; files are co-located under Providers/ (Phase 7 consolidation, spec §8.1).",
    Scope = "namespaceanddescendants",
    Target = "~N:Smart.Data.Accessor.Builders.SqlServer.Generator")]
[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "Provider generator namespace is a public FQN contract; files are co-located under Providers/ (Phase 7 consolidation, spec §8.1).",
    Scope = "namespaceanddescendants",
    Target = "~N:Smart.Data.Accessor.Builders.MySql.Generator")]
[assembly: SuppressMessage(
    "Style",
    "IDE0130:Namespace does not match folder structure",
    Justification = "Provider generator namespace is a public FQN contract; files are co-located under Providers/ (Phase 7 consolidation, spec §8.1).",
    Scope = "namespaceanddescendants",
    Target = "~N:Smart.Data.Accessor.Builders.Postgres.Generator")]
