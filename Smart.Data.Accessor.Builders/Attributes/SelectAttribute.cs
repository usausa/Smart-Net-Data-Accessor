namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Builds a full-scan <c>SELECT cols FROM t</c> statement. Columns come from the entity type
/// (or the query method's return element type), design doc §4.4.
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SelectAttribute()
    {
    }

    public SelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
