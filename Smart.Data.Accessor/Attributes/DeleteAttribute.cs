namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Builds a DELETE statement. WHERE keys come from the method's scalar value parameters;
// the entity type / table supplies metadata only.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeleteAttribute : QueryBuilderAttribute
{
    public Type? EntityType { get; }

    public string? Table { get; set; }

    public DeleteAttribute()
    {
    }

    public DeleteAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
