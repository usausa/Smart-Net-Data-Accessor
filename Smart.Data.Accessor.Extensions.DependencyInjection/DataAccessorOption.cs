namespace Smart
{
    using System.Collections.Generic;
    using System.Reflection;

    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Loaders;

    public sealed class DataAccessorOption
    {
        public ExecuteEngineConfig Config { get; } = new ExecuteEngineConfig();

        public ISqlLoader Loader { get; set; }

        public IList<Assembly> DaoAssemblies { get; } = new List<Assembly>();
    }
}
