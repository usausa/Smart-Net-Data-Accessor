namespace Smart.Data.Accessor.Builders;

using System;

// Single row SELECT by primary key (KeyAttribute).
[AttributeUsage(AttributeTargets.Method)]
public sealed class SelectSingleBuilderAttribute : Attribute
{
    public Type EntityType { get; }

    public string? Table { get; set; }

    public SelectSingleBuilderAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
