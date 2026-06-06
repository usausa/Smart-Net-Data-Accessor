namespace Smart.Data.Accessor;

using System.Collections.Concurrent;
using System.ComponentModel;

public static class DataAccessorRegistry
{
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> Factories = new();

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Register<TService>(Func<IServiceProvider, TService> factory)
        where TService : class
    {
        Factories[typeof(TService)] = factory;
    }

    public static IReadOnlyCollection<Type> RegisteredServiceTypes => Factories.Keys.ToArray();

    public static object Create(Type serviceType, IServiceProvider provider)
    {
        if (!Factories.TryGetValue(serviceType, out var factory))
        {
            throw new InvalidOperationException($"DataAccessor not registered: {serviceType}");
        }
        return factory(provider);
    }
}
