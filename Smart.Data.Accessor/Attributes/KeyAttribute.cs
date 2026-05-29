namespace Smart.Data.Accessor.Attributes;

using System;

// Marks a property as a primary-key column. Used by UpdateBuilder/DeleteBuilder/SelectBuilder
// to derive the WHERE clause automatically.
[AttributeUsage(AttributeTargets.Property)]
public sealed class KeyAttribute : Attribute
{
    public int Order { get; }

    public KeyAttribute()
    {
        Order = 0;
    }

    public KeyAttribute(int order)
    {
        Order = order;
    }
}
