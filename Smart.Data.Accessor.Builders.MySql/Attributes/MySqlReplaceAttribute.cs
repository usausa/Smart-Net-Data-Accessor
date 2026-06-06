namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL REPLACE INTO builder. Same column/value shape as INSERT; deletes-then-inserts on a duplicate key.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlReplaceAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlReplaceAttribute()
    {
    }

    public MySqlReplaceAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
