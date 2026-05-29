namespace Smart.Data.Accessor.Builders;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class DeleteBuilderAttribute : Attribute
{
    public Type EntityType { get; }

    public string? Table { get; set; }

    public DeleteBuilderAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
