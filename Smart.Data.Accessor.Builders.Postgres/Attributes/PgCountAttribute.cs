namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgCountAttribute()
    {
    }

    public PgCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
