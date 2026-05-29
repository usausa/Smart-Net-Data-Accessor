namespace Smart.Data.Accessor.Builders;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CountBuilderAttribute : Attribute
{
    public Type EntityType { get; }

    public string? Table { get; set; }

    public CountBuilderAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
