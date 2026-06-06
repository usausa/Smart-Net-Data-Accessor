namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlSelectAttribute()
    {
    }

    public MySqlSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
