namespace Smart.Data.Accessor.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlSizeAttribute : Attribute
{
    public int Size { get; }

    public SqlSizeAttribute(int size)
    {
        Size = Math.Max(size, 32);
    }
}
