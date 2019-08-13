namespace Smart.Mock
{
    using System;
    using System.Collections.Generic;

    using Smart.Data;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Loaders;

    public class GeneratorBuilder
    {
        private readonly ExecuteEngineConfig config = new ExecuteEngineConfig();

        private bool debug;

        private ISqlLoader loader;

        public GeneratorBuilder EnableDebug()
        {
            debug = true;
            return this;
        }

        public GeneratorBuilder Config(Action<ExecuteEngineConfig> action)
        {
            action(config);
            return this;
        }

        public GeneratorBuilder UseFileDatabase()
        {
            config.ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(TestDatabase.CreateConnection));
            });
            return this;
        }

        public GeneratorBuilder UseMultipleDatabase()
        {
            config.ConfigureComponents(c =>
            {
                var selector = new NamedDbProviderSelector();
                selector.AddProvider(ProviderNames.Main, new DelegateDbProvider(TestDatabase.CreateConnection));
                selector.AddProvider(ProviderNames.Sub, new DelegateDbProvider(TestDatabase.CreateConnection2));
                c.Add<IDbProviderSelector>(selector);
            });
            return this;
        }

        public GeneratorBuilder UseMemoryDatabase()
        {
            config.ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(TestDatabase.CreateMemory));
            });
            return this;
        }

        public GeneratorBuilder SetSql(string sql)
        {
            loader = new ConstLoader(sql);
            return this;
        }

        public GeneratorBuilder SetSql(Action<Dictionary<string, string>> action)
        {
            var map = new Dictionary<string, string>();
            action(map);
            loader = new MapLoader(map);
            return this;
        }

        public DaoGenerator Build()
        {
            return new DaoGenerator(config.ToEngine(), loader, debug ? new SqlDebugger() : null);
        }
    }
}
