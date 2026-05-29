namespace Smart.Data.Accessor.Builders;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TruncateBuilderAttribute : Attribute
{
    public Type EntityType { get; }

    public string? Table { get; set; }

    public TruncateBuilderAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
