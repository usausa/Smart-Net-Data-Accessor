namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Data.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public abstract class ParameterBuilderAttribute : Attribute
{
    public DbType DbType { get; }

    public int? Size { get; }

    protected ParameterBuilderAttribute()
        : this(DbType.Object, null)
    {
    }

    protected ParameterBuilderAttribute(DbType dbType)
        : this(dbType, null)
    {
    }

    protected ParameterBuilderAttribute(DbType dbType, int? size)
    {
        DbType = dbType;
        Size = size;
    }

    public virtual Action<DbParameter, object>? CreateHandler(IServiceProvider provider) => null;
}
