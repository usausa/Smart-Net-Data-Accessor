#pragma warning disable IDE0130 // Namespace does not match folder structure (M.E.DI extension method convention).
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130

using System;

using Smart.Data.Accessor;

public static class DataAccessorServiceCollectionExtensions
{
    /// <summary>
    /// Registers every accessor class discovered by the source generator (via
    /// <see cref="DataAccessorRegistry"/>) as a singleton in the M.E.DI container.
    /// The accessor's constructor parameters (<see cref="global::Smart.Data.IDbProvider"/>
    /// or <see cref="global::Smart.Data.IDbProviderSelector"/>, plus any
    /// <c>[Inject]</c>-injected services) are resolved from the container at activation time.
    /// </summary>
    public static IServiceCollection AddDataAccessors(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        foreach (var serviceType in DataAccessorRegistry.RegisteredServiceTypes)
        {
            services.AddSingleton(serviceType, sp => DataAccessorRegistry.Create(serviceType, sp));
        }
        return services;
    }
}
