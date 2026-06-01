namespace Smart.Data.Accessor;

using System;
using System.Collections.Generic;

using Smart.Data;

public sealed class DataAccessorFactoryBuilder
{
    private readonly Dictionary<Type, object> singletons = [];
    private IDbProvider? dbProvider;
    private IDbProviderSelector? providerSelector;

    /// <summary>
    /// Registers a single-source <see cref="IDbProvider"/>. Use for accessor classes
    /// that do NOT carry <c>[Provider("...")]</c>.
    /// </summary>
    public DataAccessorFactoryBuilder UseDbProvider(IDbProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        dbProvider = provider;
        return this;
    }

    /// <summary>
    /// Registers a multi-source <see cref="IDbProviderSelector"/>. Use for accessor
    /// classes that carry <c>[Provider("name")]</c>.
    /// </summary>
    public DataAccessorFactoryBuilder UseDbProviderSelector(IDbProviderSelector selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        providerSelector = selector;
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
        if (dbProvider is null && providerSelector is null)
        {
            throw new InvalidOperationException("At least one of UseDbProvider / UseDbProviderSelector is required.");
        }
        return new DataAccessorFactory(dbProvider, providerSelector, singletons);
    }
}
