namespace Smart.Data.Accessor;

public sealed class DataAccessorFactoryBuilder
{
    private readonly Dictionary<Type, object> singletons = [];
    private IDbProvider? dbProvider;
    private IDbProviderSelector? providerSelector;

    // Registers a single-source IDbProvider. Use for accessor classes that do NOT carry [Provider("...")].
    public DataAccessorFactoryBuilder UseDbProvider(IDbProvider provider)
    {
        dbProvider = provider;
        return this;
    }

    // Registers a multi-source IDbProviderSelector. Use for accessor classes that carry [Provider("name")].
    public DataAccessorFactoryBuilder UseDbProviderSelector(IDbProviderSelector selector)
    {
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
        if ((dbProvider is null) && (providerSelector is null))
        {
            throw new InvalidOperationException("At least one of UseDbProvider / UseDbProviderSelector is required.");
        }
        return new DataAccessorFactory(dbProvider, providerSelector, singletons);
    }
}
