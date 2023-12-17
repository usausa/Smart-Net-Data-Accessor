namespace Smart.Data.Accessor.Extensions.DependencyInjection;

using Smart.Data.Accessor.Engine;

public sealed class ExecuteEngineFactory
{
    private readonly ExecuteEngineConfig config;

    public ExecuteEngineFactory(ExecuteEngineFactoryOptions options, IServiceProvider serviceProvider)
    {
        config = new ExecuteEngineConfig();
        config.SetServiceProvider(serviceProvider);

        if (options.TypeMapConfig is not null)
        {
            config.ConfigureTypeMap(options.TypeMapConfig);
        }

        if (options.TypeHandlersConfig is not null)
        {
            config.ConfigureTypeHandlers(options.TypeHandlersConfig);
        }

        if (options.ResultMapperFactoriesConfig is not null)
        {
            config.ConfigureResultMapperFactories(options.ResultMapperFactoriesConfig);
        }
    }

    public ExecuteEngine Create() => config.ToEngine();
}
