namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// PostgreSQL upsert builder: INSERT ... ON CONFLICT (key) DO UPDATE SET col = EXCLUDED.col (or DO NOTHING when there
// is nothing to update). Matches on the [Key] columns; updates the non-key, non-[DatabaseManaged] columns. Entity mode only.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class PgUpsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public PgUpsertAttribute()
    {
    }

    public PgUpsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
