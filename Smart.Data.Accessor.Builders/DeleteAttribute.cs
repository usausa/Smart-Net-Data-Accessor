namespace Smart.Data.Accessor.Builders;

using System;

/// <summary>
/// Builds a <c>DELETE</c> statement. WHERE keys come from the method's scalar value parameters;
/// the entity type / table supplies metadata only (design doc §4.4).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
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
