namespace Smart.Data.Accessor.Builders;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SelectBuilderAttribute : Attribute
{
    public Type EntityType { get; }

    public string? Table { get; set; }

    public SelectBuilderAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
