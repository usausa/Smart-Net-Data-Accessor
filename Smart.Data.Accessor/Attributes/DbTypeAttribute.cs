namespace Smart.Data.Accessor.Attributes;

using System;
using System.Data;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class DbTypeAttribute : Attribute
{
    public DbType DbType { get; }

    public DbTypeAttribute(DbType dbType)
    {
        DbType = dbType;
    }
}
