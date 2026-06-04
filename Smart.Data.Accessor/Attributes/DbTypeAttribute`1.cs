namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Provider-specific DbType attribute. Pair with a provider-specific enum like
// Microsoft.Data.SqlClient.SqlDbType, MySql.Data.MySqlClient.MySqlDbType, NpgsqlTypes.NpgsqlDbType,
// or Oracle.ManagedDataAccess.Client.OracleDbType. The Generator casts the underlying DbParameter
// to the matching provider parameter type and sets its native property.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class DbTypeAttribute<TEnum> : Attribute
    where TEnum : struct, Enum
{
    public TEnum DbType { get; }

    public DbTypeAttribute(TEnum dbType)
    {
        DbType = dbType;
    }
}
