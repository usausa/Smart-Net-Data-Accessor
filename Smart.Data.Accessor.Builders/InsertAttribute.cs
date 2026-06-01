namespace Smart.Data.Accessor.Builders;

using System;

/// <summary>
/// Builds an <c>INSERT</c> statement. Entity mode (<c>[Insert(typeof(T))]</c>) derives columns
/// from the entity type; parameter mode (<c>[Insert(Table = "...")]</c>) derives columns from
/// the method's value parameters (design doc §4.4).
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
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
