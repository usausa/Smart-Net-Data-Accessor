namespace Smart.Data.Accessor.Extensions.DependencyInjection
{
    using System.Collections.Generic;
    using System.Reflection;

    using Smart.Data.Accessor.Loaders;

    public sealed class DataAccessorOption
    {
        public ExecuteEngineFactoryOptions EngineOption { get; } = new ExecuteEngineFactoryOptions();

        public ISqlLoader Loader { get; set; }

        public IList<Assembly> DaoAssemblies { get; } = new List<Assembly>();
    }
}
