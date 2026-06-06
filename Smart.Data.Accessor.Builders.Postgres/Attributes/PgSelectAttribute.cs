namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgSelectAttribute()
    {
    }

    public PgSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
