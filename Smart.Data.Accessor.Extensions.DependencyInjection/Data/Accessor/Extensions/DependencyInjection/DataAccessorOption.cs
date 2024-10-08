namespace Smart.Data.Accessor.Extensions.DependencyInjection;

using System.Reflection;

public sealed class DataAccessorOption
{
    public ExecuteEngineFactoryOptions EngineOption { get; } = new();

    public IList<Assembly> AccessorAssemblies { get; } = [];
}
