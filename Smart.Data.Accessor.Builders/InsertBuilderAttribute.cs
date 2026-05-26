namespace Smart.Data.Accessor.Builders;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class InsertBuilderAttribute : Attribute
{
    public Type EntityType { get; }

    public string? Table { get; set; }

    public InsertBuilderAttribute(Type entityType)
    {
        EntityType = entityType;
    }
}
