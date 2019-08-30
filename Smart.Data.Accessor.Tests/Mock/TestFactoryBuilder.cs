namespace Smart.Mock
{
    using System;
    using System.Collections.Generic;

    using Smart.Data;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Generator;

    public class TestFactoryBuilder
    {
        private readonly ExecuteEngineConfig config = new ExecuteEngineConfig();

        private ISqlLoader loader;

        private IGeneratorOption option;

        public TestFactoryBuilder Config(Action<ExecuteEngineConfig> action)
        {
            action(config);
            return this;
        }

        public TestFactoryBuilder UseFileDatabase()
        {
            config.ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(TestDatabase.CreateConnection));
            });
            return this;
        }

        public TestFactoryBuilder UseMultipleDatabase()
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

        public TestFactoryBuilder UseMemoryDatabase()
        {
            config.ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(TestDatabase.CreateMemory));
            });
            return this;
        }

        public TestFactoryBuilder SetSql(string sql)
        {
            loader = new ConstLoader(sql);
            return this;
        }

        public TestFactoryBuilder SetSql(Action<Dictionary<string, string>> action)
        {
            var map = new Dictionary<string, string>();
            action(map);
            loader = new MapLoader(map);
            return this;
        }

        public TestFactoryBuilder ConfigureOptions(Action<Dictionary<string, string>> action)
        {
            var map = new Dictionary<string, string>();
            action(map);
            option = new TestGeneratorOption(map);
            return this;
        }

        public TestFactory Build()
        {
            return new TestFactory(loader, option, config.ToEngine());
        }
    }
}
