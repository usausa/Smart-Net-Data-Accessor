namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public sealed class DbTypeAttribute : Attribute
{
    public DbType DbType { get; }

    public DbTypeAttribute(DbType dbType)
    {
        DbType = dbType;
    }
}
