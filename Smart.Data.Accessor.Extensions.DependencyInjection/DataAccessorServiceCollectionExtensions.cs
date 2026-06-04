#pragma warning disable IDE0130 // Namespace does not match folder structure (M.E.DI extension method convention).
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130
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
            services.AddSingleton(serviceType, sp => DataAccessorRegistry.Create(serviceType, sp));
        }
        return services;
    }
}
