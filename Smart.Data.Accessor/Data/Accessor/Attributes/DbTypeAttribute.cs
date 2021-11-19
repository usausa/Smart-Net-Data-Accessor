namespace Smart.Data.Accessor.Attributes;

using System.Data;

public sealed class DbTypeAttribute : ParameterBuilderAttribute
{
    public DbTypeAttribute(DbType dbType)
        : base(dbType)
    {
    }

    public DbTypeAttribute(DbType dbType, int size)
        : base(dbType, size)
    {
    }
}
