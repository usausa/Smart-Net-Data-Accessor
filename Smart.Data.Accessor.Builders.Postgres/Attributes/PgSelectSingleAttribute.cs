namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgSelectSingleAttribute()
    {
    }

    public PgSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
