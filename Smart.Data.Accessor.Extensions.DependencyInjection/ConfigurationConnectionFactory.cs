namespace Smart.Data.Accessor.Extensions.DependencyInjection;

using System;
using System.Data.Common;

using Microsoft.Extensions.Configuration;

using Smart.Data.Accessor.Connection;

public sealed class ConfigurationConnectionFactory : IConnectionFactory
{
    private const string DefaultConnectionStringName = "Default";

    private readonly IConfiguration configuration;

    private readonly Func<string, DbConnection> dbConnectionFactory;

    public ConfigurationConnectionFactory(
        IConfiguration configuration,
        Func<string, DbConnection> dbConnectionFactory)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(dbConnectionFactory);
        this.configuration = configuration;
        this.dbConnectionFactory = dbConnectionFactory;
    }

    public DbConnection Create(string? providerName)
    {
        var name = providerName ?? DefaultConnectionStringName;
        var connectionString = configuration.GetSection("ConnectionStrings")[name]
            ?? throw new InvalidOperationException(
                $"Connection string '{name}' not found in configuration section 'ConnectionStrings'.");
        return dbConnectionFactory(connectionString);
    }
}
