namespace Smart.Data.Accessor;

using System;
using System.Collections.Generic;

using Smart.Data.Accessor.Connection;

public sealed class DataAccessorFactory : IServiceProvider
{
    private readonly IConnectionFactory connectionFactory;
    private readonly Dictionary<Type, object> singletons;
    private readonly Dictionary<Type, object> accessorCache = [];

    internal DataAccessorFactory(
        IConnectionFactory connectionFactory,
        Dictionary<Type, object> singletons)
    {
        this.connectionFactory = connectionFactory;
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
        if (serviceType == typeof(IConnectionFactory))
        {
            return connectionFactory;
        }
        return singletons.TryGetValue(serviceType, out var v) ? v : null;
    }
}
