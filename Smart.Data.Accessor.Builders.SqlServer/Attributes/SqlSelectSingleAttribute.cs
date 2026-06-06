namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// SQL Server keyed SELECT builder (WHERE from value parameters).
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlSelectSingleAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public SqlSelectSingleAttribute()
    {
    }

    public SqlSelectSingleAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
