namespace Smart.Data.Accessor.Resolver;

using System.Diagnostics.CodeAnalysis;

using Smart.Resolver;

public static class ResolverConfigExtensions
{
    /// <summary>
    /// Registers every accessor discovered by the source generator (via
    /// <see cref="DataAccessorRegistry"/>) into the Smart.Resolver container as a singleton,
    /// plus an <see cref="IDbProviderSelector"/> backed by the resolver for multi-source
    /// (<c>[Provider("name")]</c>) accessors. The accessor's constructor dependencies
    /// (<see cref="IDbProvider"/> / <see cref="IDbProviderSelector"/> and any <c>[Inject]</c>
    /// services) are resolved from the container at activation time via
    /// <see cref="ServiceProviderAdapter"/>.
    /// </summary>
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
