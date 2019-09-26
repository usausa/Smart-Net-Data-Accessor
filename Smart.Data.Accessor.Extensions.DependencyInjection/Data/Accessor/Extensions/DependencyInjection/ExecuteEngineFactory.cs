namespace Smart.Data.Accessor.Extensions.DependencyInjection
{
    using System;

    using Smart.Data.Accessor.Engine;

    public class ExecuteEngineFactory
    {
        private readonly ExecuteEngineConfig config;

        public ExecuteEngineFactory(ExecuteEngineFactoryOptions options, IServiceProvider serviceProvider)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            config = new ExecuteEngineConfig();
            config.SetServiceProvider(serviceProvider);

            if (options.TypeMapConfig != null)
            {
                config.ConfigureTypeMap(options.TypeMapConfig);
            }

            if (options.TypeHandlersConfig != null)
            {
                config.ConfigureTypeHandlers(options.TypeHandlersConfig);
            }

            if (options.ResultMapperFactoriesConfig != null)
            {
                config.ConfigureResultMapperFactories(options.ResultMapperFactoriesConfig);
            }
        }

        public ExecuteEngine Create() => config.ToEngine();
    }
}
