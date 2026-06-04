namespace Smart.Data.Accessor.Resolver;

using System.Diagnostics.CodeAnalysis;

using Smart.Resolver;

public static class ResolverConfigExtensions
{
    // Registers every accessor discovered by the source generator (via DataAccessorRegistry) into the
    // Smart.Resolver container as a singleton, plus an IDbProviderSelector backed by the resolver for
    // multi-source ([Provider("name")]) accessors. The accessor's constructor dependencies (IDbProvider
    // / IDbProviderSelector and any [Inject] services) are resolved from the container at activation
    // time via ServiceProviderAdapter.
    [UnconditionalSuppressMessage("Trimming", "IL2072:DynamicallyAccessedMembers", Justification = "The bound accessor type is activated only through the ToMethod factory (DataAccessorRegistry.Create), which calls a source-generated `new` constructor rooted by the [ModuleInitializer]. Smart.Resolver never reflects over the type's constructors/properties, so the DynamicallyAccessedMembers requirement on ResolverConfig.Bind(Type) is satisfied for this usage.")]
    public static ResolverConfig UseDataAccessors(this ResolverConfig config)
    {
        config.Bind<IDbProviderSelector>().To<ResolverDbProviderSelector>().InSingletonScope();

        foreach (var serviceType in DataAccessorRegistry.RegisteredServiceTypes)
        {
            config.Bind(serviceType)
                .ToMethod(resolver => DataAccessorRegistry.Create(serviceType, new ServiceProviderAdapter(resolver)))
                .InSingletonScope();
        }

        return config;
    }
}
