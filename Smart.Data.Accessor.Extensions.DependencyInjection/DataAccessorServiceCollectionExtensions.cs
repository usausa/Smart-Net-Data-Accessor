#pragma warning disable IDE0130 // Namespace does not match folder structure (M.E.DI extension method convention).
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

using System;
using System.Data.Common;

using Microsoft.Extensions.Configuration;

using Smart.Data.Accessor;
using Smart.Data.Accessor.Connection;
using Smart.Data.Accessor.Extensions.DependencyInjection;

public static class DataAccessorServiceCollectionExtensions
{
    public static IServiceCollection AddDataAccessors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        foreach (var serviceType in DataAccessorRegistry.RegisteredServiceTypes)
        {
            services.AddSingleton(serviceType, sp => DataAccessorRegistry.Create(serviceType, sp));
        }
        return services;
    }

    public static IServiceCollection AddDataAccessorConfigurationConnectionFactory(
        this IServiceCollection services,
        Func<string, DbConnection> dbConnectionFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(dbConnectionFactory);
        services.AddSingleton<IConnectionFactory>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            return new ConfigurationConnectionFactory(configuration, dbConnectionFactory);
        });
        return services;
    }
}
