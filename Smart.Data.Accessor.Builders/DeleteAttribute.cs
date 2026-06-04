namespace Smart.Data.Accessor.Builders;

using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Builds a <c>DELETE</c> statement. WHERE keys come from the method's scalar value parameters;
/// the entity type / table supplies metadata only (design doc §4.4).
/// </summary>
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
