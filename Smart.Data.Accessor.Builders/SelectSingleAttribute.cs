namespace Smart.Data.Accessor.Builders;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Builds a <c>SELECT cols FROM t WHERE key=@param</c> statement. Columns from the entity type
/// (or return element type); WHERE keys from the method's scalar value parameters (design doc §4.4).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class SelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SelectSingleAttribute()
    {
    }

    public SelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
