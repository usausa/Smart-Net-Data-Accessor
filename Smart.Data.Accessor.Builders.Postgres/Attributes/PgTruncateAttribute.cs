namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL TRUNCATE TABLE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgTruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgTruncateAttribute()
    {
    }

    public PgTruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
