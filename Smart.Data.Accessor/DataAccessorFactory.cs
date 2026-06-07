namespace Smart.Data.Accessor;

using System.Collections.Concurrent;

public sealed class DataAccessorFactory : IServiceProvider
{
    private readonly IDbProvider? dbProvider;
    private readonly IDbProviderSelector? providerSelector;
    private readonly Dictionary<Type, object> singletons;
    private readonly ConcurrentDictionary<Type, object> accessorCache = new();

    internal DataAccessorFactory(
        IDbProvider? dbProvider,
        IDbProviderSelector? providerSelector,
        Dictionary<Type, object> singletons)
    {
        this.dbProvider = dbProvider;
        this.providerSelector = providerSelector;
        this.singletons = singletons;
    }

    public T Create<T>()
        where T : class
        => (T)accessorCache.GetOrAdd(typeof(T), static (t, self) => DataAccessorRegistry.Create(t, self), this);

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IDbProvider))
        {
            return dbProvider;
        }
        if (serviceType == typeof(IDbProviderSelector))
        {
            return providerSelector;
        }
        return singletons.GetValueOrDefault(serviceType);
    }
}
