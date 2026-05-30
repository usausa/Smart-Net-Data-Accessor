namespace Smart.Data.Accessor.Attributes;

using System;

/// <summary>
/// Provider-specific DbType attribute (spec §1.4 F15 / §5.3 / §5.3.1).
/// Pair with a provider-specific enum like <c>Microsoft.Data.SqlClient.SqlDbType</c>,
/// <c>MySql.Data.MySqlClient.MySqlDbType</c>, <c>NpgsqlTypes.NpgsqlDbType</c>,
/// or <c>Oracle.ManagedDataAccess.Client.OracleDbType</c>. The Generator casts the
/// underlying <see cref="System.Data.Common.DbParameter"/> to the matching provider
/// parameter type and sets its native property.
/// </summary>
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
