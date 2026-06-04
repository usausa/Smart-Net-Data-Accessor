namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Builds a TRUNCATE TABLE t statement. The entity type / table supplies the table name only.
// SQLite users should use raw SQL (DELETE FROM) instead.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class TruncateAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public TruncateAttribute()
    {
    }

    public TruncateAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
