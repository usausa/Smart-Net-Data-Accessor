namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Builds an INSERT statement. Entity mode ([Insert(typeof(T))]) derives columns from the entity
// type; parameter mode ([Insert(Table = "...")]) derives columns from the method's value parameters.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class InsertAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public InsertAttribute()
    {
    }

    public InsertAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
