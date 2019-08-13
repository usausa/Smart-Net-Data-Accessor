namespace Smart.Data.Accessor.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using System.Reflection;

    public sealed class DataAccessorOption
    {
        public ExecuteEngineFactoryOptions EngineOption { get; } = new ExecuteEngineFactoryOptions();

        public IList<Assembly> DaoAssemblies { get; } = new List<Assembly>();
    }
}
