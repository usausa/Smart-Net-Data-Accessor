namespace Smart.Data.Accessor.Resolver.Components;

using Smart.Resolver;

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
