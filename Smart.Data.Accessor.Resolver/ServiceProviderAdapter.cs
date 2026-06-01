namespace Smart.Data.Accessor.Resolver;

using System;

using Smart.Resolver;

/// <summary>
/// Adapts an <see cref="IResolver"/> to <see cref="IServiceProvider"/> so that
/// <see cref="DataAccessorRegistry.Create"/> (which resolves an accessor's constructor
/// dependencies through <see cref="IServiceProvider.GetService"/>) can be backed by the
/// Smart.Resolver container.
/// </summary>
public sealed class ServiceProviderAdapter : IServiceProvider
{
    private readonly IResolver resolver;

    public ServiceProviderAdapter(IResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        this.resolver = resolver;
    }

    // IServiceProvider contract: return null for an unregistered service (matches the M.E.DI path).
    public object? GetService(Type serviceType) =>
        resolver.TryGet(serviceType, out var service) ? service : null;
}
