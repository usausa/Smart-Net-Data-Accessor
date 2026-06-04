namespace Smart.Data.Accessor.Resolver;

using Smart.Resolver;

// Multi-source provider selector (Pattern B with [Provider("name")]). The generated accessor calls
// providerSelector.GetProvider("name"); this resolves the keyed IDbProvider from the Smart.Resolver
// container (resolver.Get<IDbProvider>(name)).
public sealed class ResolverDbProviderSelector : IDbProviderSelector
{
    private readonly IResolver resolver;

    public ResolverDbProviderSelector(IResolver resolver)
    {
        this.resolver = resolver;
    }

    public IDbProvider GetProvider(object parameter)
    {
        return resolver.Get<IDbProvider>((string)parameter);
    }
}
