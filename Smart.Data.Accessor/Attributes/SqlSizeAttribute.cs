namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class SqlSizeAttribute : Attribute
{
    public int Size { get; }

    public SqlSizeAttribute(int size)
    {
        Size = size;
    }
}
