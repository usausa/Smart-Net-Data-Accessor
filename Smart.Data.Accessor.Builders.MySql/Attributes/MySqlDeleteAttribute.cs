namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL DELETE builder.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlDeleteAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlDeleteAttribute()
    {
    }

    public MySqlDeleteAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
