namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class SqlSizeAttribute : Attribute
{
    public int Size { get; }

    public SqlSizeAttribute(int size)
    {
        Size = size;
    }
}
