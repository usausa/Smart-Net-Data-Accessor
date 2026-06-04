namespace Smart.Data.Accessor.Builders;

using System;

/// <summary>
/// Marker base for all query-builder attributes (Insert / Update / Delete / Count /
/// Select / SelectSingle / Truncate). Carries no metadata itself — the core Source
/// Generator only checks whether a method attribute derives from this type. Each derived
/// attribute declares the metadata (EntityType / Table) it needs (design doc §4.1).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public abstract class QueryBuilderAttribute : Attribute
{
}
