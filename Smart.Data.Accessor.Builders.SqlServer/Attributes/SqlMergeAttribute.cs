namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// SQL Server MERGE-based UPSERT builder. Matches on [Key] columns; WHEN MATCHED updates the non-key,
// non-[DatabaseManaged] columns and WHEN NOT MATCHED inserts. Entity mode only.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlMergeAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlMergeAttribute()
    {
    }

    public SqlMergeAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
