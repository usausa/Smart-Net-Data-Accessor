namespace Smart.Data.Accessor;

using System;
using System.Collections.Generic;
using System.ComponentModel;

public static class DataAccessorRegistry
{
    private static readonly Dictionary<Type, Func<IServiceProvider, object>> Factories = [];

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Register<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        Factories[typeof(TService)] = sp => factory(sp);
    }

    public static IReadOnlyCollection<Type> RegisteredServiceTypes => Factories.Keys;

    public static object Create(Type serviceType, IServiceProvider provider)
    {
        if (!Factories.TryGetValue(serviceType, out var factory))
        {
            throw new InvalidOperationException($"DataAccessor not registered: {serviceType}");
        }
        return factory(provider);
    }
}
