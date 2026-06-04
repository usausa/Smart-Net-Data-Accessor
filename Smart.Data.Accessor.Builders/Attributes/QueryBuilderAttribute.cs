namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Marker base for all query-builder attributes (Insert / Update / Delete / Count / Select /
// SelectSingle / Truncate). Carries no metadata itself — the core Source Generator only checks
// whether a method attribute derives from this type. Each derived attribute declares the metadata
// (EntityType / Table) it needs.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public abstract class QueryBuilderAttribute : Attribute
{
}
