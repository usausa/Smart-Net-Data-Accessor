namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class DbTypeAttribute : Attribute
{
    public DbType DbType { get; }

    public DbTypeAttribute(DbType dbType)
    {
        DbType = dbType;
    }
}
