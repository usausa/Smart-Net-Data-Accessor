namespace Smart.Data.Accessor.Resolver;

using System;

using Smart.Data;
using Smart.Resolver;

public static class ResolverConfigExtensions
{
    /// <summary>
    /// Registers every accessor discovered by the source generator (via
    /// <see cref="DataAccessorRegistry"/>) into the Smart.Resolver container as a singleton,
    /// plus an <see cref="IDbProviderSelector"/> backed by the resolver for multi-source
    /// (<c>[Provider("name")]</c>) accessors. The accessor's constructor dependencies
    /// (<see cref="IDbProvider"/> / <see cref="IDbProviderSelector"/> and any <c>[Inject]</c>
    /// services) are resolved from the container at activation time via
    /// <see cref="ServiceProviderAdapter"/>.
    /// </summary>
    public static ResolverConfig UseDataAccessors(this ResolverConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        config.Bind<IDbProviderSelector>().To<ResolverDbProviderSelector>().InSingletonScope();

        foreach (var serviceType in DataAccessorRegistry.RegisteredServiceTypes)
        {
            config.Bind(serviceType)
                .ToMethod(resolver => DataAccessorRegistry.Create(serviceType, new ServiceProviderAdapter(resolver)))
                .InSingletonScope();
        }

        return config;
    }
}
