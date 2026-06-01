namespace Smart.Data.Accessor;

using System;
using System.Collections.Generic;

using Smart.Data;

public sealed class DataAccessorFactory : IServiceProvider
{
    private readonly IDbProvider? dbProvider;
    private readonly IDbProviderSelector? providerSelector;
    private readonly Dictionary<Type, object> singletons;
    private readonly Dictionary<Type, object> accessorCache = [];

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
    {
        if (!accessorCache.TryGetValue(typeof(T), out var accessor))
        {
            accessor = DataAccessorRegistry.Create(typeof(T), this);
            accessorCache[typeof(T)] = accessor;
        }
        return (T)accessor;
    }

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
        return singletons.TryGetValue(serviceType, out var v) ? v : null;
    }
}
