namespace Smart.Data.Accessor.Extensions.DependencyInjection;

using System.Collections.Generic;
using System.Reflection;

public sealed class DataAccessorOption
{
    public ExecuteEngineFactoryOptions EngineOption { get; } = new();

    // ReSharper disable once CollectionNeverUpdated.Global
    public IList<Assembly> AccessorAssemblies { get; } = new List<Assembly>();
}
