namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL TRUNCATE TABLE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlTruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlTruncateAttribute()
    {
    }

    public MySqlTruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
