namespace Smart.Data.Accessor.Resolver;

using Smart.Resolver;

// Adapts an IResolver to IServiceProvider so that DataAccessorRegistry.Create (which resolves an
// accessor's constructor dependencies through IServiceProvider.GetService) can be backed by the
// Smart.Resolver container.
public sealed class ServiceProviderAdapter : IServiceProvider
{
    private readonly IResolver resolver;

    public ServiceProviderAdapter(IResolver resolver)
    {
        this.resolver = resolver;
    }

    // IServiceProvider contract: return null for an unregistered service (matches the M.E.DI path).
    public object? GetService(Type serviceType) =>
        resolver.TryGet(serviceType, out var service) ? service : null;
}
