namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class TypeMapAttribute : Attribute
{
    public Type ClrType { get; }

    public DbType DbType { get; }

    public int? Size { get; set; }

    public TypeMapAttribute(Type clrType, DbType dbType)
    {
        ClrType = clrType;
        DbType = dbType;
    }
}
