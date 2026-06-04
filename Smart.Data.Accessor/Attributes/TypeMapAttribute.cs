namespace Smart.Data.Accessor.Attributes;

using System;
using System.Data;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
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
