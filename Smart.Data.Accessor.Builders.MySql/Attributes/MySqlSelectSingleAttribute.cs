namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// MySQL keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MySqlSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public MySqlSelectSingleAttribute()
    {
    }

    public MySqlSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
