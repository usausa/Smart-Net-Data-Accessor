namespace Smart.Data.Accessor.Resolver;

using Smart.Resolver;

/// <summary>
/// Multi-source provider selector (Pattern B with <c>[Provider("name")]</c>). The generated accessor
/// calls <c>providerSelector.GetProvider("name")</c>; this resolves the keyed
/// <see cref="IDbProvider"/> from the Smart.Resolver container (<c>resolver.Get&lt;IDbProvider&gt;(name)</c>).
/// </summary>
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
