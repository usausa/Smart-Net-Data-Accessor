namespace Smart.Data.Accessor;

using System;
using System.Collections.Generic;

using Smart.Data.Accessor.Connection;

public sealed class DataAccessorFactoryBuilder
{
    private readonly Dictionary<Type, object> singletons = [];
    private IConnectionFactory? connectionFactory;

    public DataAccessorFactoryBuilder UseConnectionFactory(IConnectionFactory factory)
    {
        connectionFactory = factory;
        return this;
    }

    public DataAccessorFactoryBuilder AddSingleton<T>(T instance)
        where T : class
    {
        singletons[typeof(T)] = instance;
        return this;
    }

    public DataAccessorFactory Build()
    {
        if (connectionFactory is null)
        {
            throw new InvalidOperationException("IConnectionFactory is required.");
        }
        return new DataAccessorFactory(connectionFactory, singletons);
    }
}
