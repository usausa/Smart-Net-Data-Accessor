namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// SQL Server SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlCountAttribute()
    {
    }

    public SqlCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
