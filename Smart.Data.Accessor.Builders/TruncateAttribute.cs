namespace Smart.Data.Accessor.Builders;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Builds a <c>TRUNCATE TABLE t</c> statement. The entity type / table supplies the table name only
/// (design doc §4.4). SQLite users should use raw SQL (DELETE FROM) instead.
/// </summary>
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
