namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Builds a SELECT columns FROM t WHERE key=@param statement. Columns from the entity type
// (or return element type); WHERE keys from the method's scalar value parameters.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
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
