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
        public static ResolverConfig UseDataAccessor(this ResolverConfig config)
        {
            return UseDataAccessor(config, null);
        }

        public static ResolverConfig UseDataAccessor(this ResolverConfig config, Action<ExecuteEngineFactoryOptions> action)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var options = new ExecuteEngineFactoryOptions();
            action?.Invoke(options);

            config.Bind<ExecuteEngine>().ToMethod(r =>
            {
                var engineConfig = new ExecuteEngineConfig();
                engineConfig.SetServiceProvider(new ServiceProviderAdapter(r));

                if (options.TypeMapConfig != null)
                {
                    engineConfig.ConfigureTypeMap(options.TypeMapConfig);
                }

                if (options.TypeHandlersConfig != null)
                {
                    engineConfig.ConfigureTypeHandlers(options.TypeHandlersConfig);
                }

                if (options.ResultMapperFactoriesConfig != null)
                {
                    engineConfig.ConfigureResultMapperFactories(options.ResultMapperFactoriesConfig);
                }

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
