namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL INSERT IGNORE builder. Same column/value shape as INSERT; silently skips rows that violate a unique key.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlInsertIgnoreAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlInsertIgnoreAttribute()
    {
    }

    public MySqlInsertIgnoreAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
