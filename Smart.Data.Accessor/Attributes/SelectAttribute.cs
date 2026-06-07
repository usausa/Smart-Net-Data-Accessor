namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Builds a full-scan SELECT columns FROM t statement. Columns come from the entity type
// (or the query method's return element type).
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
