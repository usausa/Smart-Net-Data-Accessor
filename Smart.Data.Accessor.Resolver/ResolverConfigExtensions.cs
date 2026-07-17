namespace Smart.Data.Accessor.Resolver;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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

    // アクセサ群を別アセンブリ(データ層ライブラリ等)に置いた構成用。そのアセンブリの型に一切触れる前に
    // 引数なしの UseDataAccessors() を呼ぶと、モジュール初期化子が未実行のため登録が黙って 0 件になる。
    // 対象アセンブリを明示すると、先にモジュール初期化子を実行してから登録する。
    // For layouts where the accessors live in a separate assembly (a data-layer library): calling the
    // parameterless UseDataAccessors() before touching any type of that assembly silently registers
    // nothing (its module initializer has not run yet). Passing the assemblies runs their module
    // initializers first and then registers.
    public static ResolverConfig UseDataAccessors(this ResolverConfig config, params Assembly[] accessorAssemblies)
    {
        DataAccessorRegistry.EnsureRegistered(accessorAssemblies);
        return UseDataAccessors(config);
    }
}
