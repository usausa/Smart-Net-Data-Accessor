namespace Smart.Data.Accessor.Connection;

using System;
using System.Data.Common;

public sealed class SingleConnectionFactory : IConnectionFactory
{
    private readonly DbConnection connection;

    public SingleConnectionFactory(DbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        this.connection = connection;
    }

    public DbConnection Create(string? providerName)
    {
        _ = providerName;
        return connection;
    }
}
