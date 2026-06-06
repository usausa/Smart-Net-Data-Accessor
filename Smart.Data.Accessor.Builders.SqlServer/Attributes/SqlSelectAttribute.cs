namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// SQL Server full-scan SELECT builder (supports [Limit]/[Offset] paging).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlSelectAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlSelectAttribute()
    {
    }

    public SqlSelectAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
