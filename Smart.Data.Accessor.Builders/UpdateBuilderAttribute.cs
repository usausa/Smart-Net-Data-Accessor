namespace Smart.Data.Accessor.Builders;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class UpdateBuilderAttribute : Attribute
{
    public Type EntityType { get; }

    public string? Table { get; set; }

    public UpdateBuilderAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
