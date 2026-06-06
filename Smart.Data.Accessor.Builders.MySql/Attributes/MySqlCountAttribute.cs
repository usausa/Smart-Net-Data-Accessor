namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL SELECT COUNT(*) builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlCountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlCountAttribute()
    {
    }

    public MySqlCountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
