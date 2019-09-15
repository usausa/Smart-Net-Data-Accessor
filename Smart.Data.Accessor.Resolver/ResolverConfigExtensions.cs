namespace Smart.Data.Accessor.Resolver
{
    using System;

    using Smart.Converter;
    using Smart.Data.Accessor.Dialect;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Resolver.Components;
    using Smart.Data.Accessor.Resolver.Handlers;
    using Smart.Data.Accessor.Selectors;
    using Smart.Resolver;

    public static class ResolverConfigExtensions
    {
        public static ResolverConfig UseAccessor(this ResolverConfig config)
        {
            return UseAccessor(config, null);
        }

        public static ResolverConfig UseAccessor(this ResolverConfig config, Action<ExecuteEngineConfig> action)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var engineConfig = new ExecuteEngineConfig();
            action?.Invoke(engineConfig);

            config.Bind<ExecuteEngine>().ToMethod(p =>
            {
                engineConfig.SetServiceProvider(p.Get<IServiceProvider>());
                return engineConfig.ToEngine();
            }).InSingletonScope();

            config.Bind<DataAccessorFactory>().ToSelf().InSingletonScope();

            config.Bind<IObjectConverter>().ToConstant(ObjectConverter.Default).InSingletonScope();
            config.Bind<IPropertySelector>().ToConstant(DefaultPropertySelector.Instance).InSingletonScope();
            config.Bind<IEmptyDialect>().To<EmptyDialect>().InSingletonScope();

            config.Bind<IDbProviderSelector>().To<ResolverDbProviderSelector>().InSingletonScope();

            config.UseMissingHandler<AccessorMissingHandler>();

            return config;
        }
    }
}
