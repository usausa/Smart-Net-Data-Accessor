namespace Smart.Data.Accessor.Attributes;

using System;
using System.Data;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public abstract class ParameterBuilderAttribute : Attribute
{
    public DbType DbType { get; }

    public int? Size { get;  }

    protected ParameterBuilderAttribute(DbType dbType)
        : this(dbType, null)
    {
    }

    protected ParameterBuilderAttribute(DbType dbType, int? size)
    {
        DbType = dbType;
        Size = size;
    }
}
