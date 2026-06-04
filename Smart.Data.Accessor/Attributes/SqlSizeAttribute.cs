namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public sealed class SqlSizeAttribute : Attribute
{
    public int Size { get; }

    public SqlSizeAttribute(int size)
    {
        Size = size;
    }
}
