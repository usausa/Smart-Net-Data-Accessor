namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL upsert builder: INSERT ... ON DUPLICATE KEY UPDATE. Inserts the entity; on a duplicate key it updates the
// non-key, non-[DatabaseManaged] columns. Entity mode only.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlUpsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlUpsertAttribute()
    {
    }

    public MySqlUpsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
