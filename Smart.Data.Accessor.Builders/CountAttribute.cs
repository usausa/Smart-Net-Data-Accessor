namespace Smart.Data.Accessor.Builders;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Builds a <c>SELECT COUNT(*)</c> statement. The entity type / table supplies the table name only
/// (design doc §4.4).
/// </summary>
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class CountAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public CountAttribute()
    {
    }

    public CountAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
