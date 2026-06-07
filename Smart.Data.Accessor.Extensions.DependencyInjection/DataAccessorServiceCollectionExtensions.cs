#pragma warning disable IDE0130 // Namespace does not match folder structure (M.E.DI extension method convention).
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

using Smart.Data.Accessor;

public static class DataAccessorServiceCollectionExtensions
{
    // Registers every accessor class discovered by the source generator (via DataAccessorRegistry)
    // as a singleton in the M.E.DI container. The accessor's constructor parameters (IDbProvider or
    // IDbProviderSelector, plus any [Inject]-injected services) are resolved from the container at
    // activation time.
    public static IServiceCollection AddDataAccessors(this IServiceCollection services)
    {
        foreach (var serviceType in DataAccessorRegistry.RegisteredServiceTypes)
        {
            services.AddSingleton(serviceType, provider => DataAccessorRegistry.Create(serviceType, provider));
        }
        return services;
    }
}
