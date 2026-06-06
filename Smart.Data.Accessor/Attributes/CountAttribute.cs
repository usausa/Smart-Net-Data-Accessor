namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Builds a SELECT COUNT(*) statement. The entity type / table supplies the table name only.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class CountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public CountAttribute()
    {
    }

    public CountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
