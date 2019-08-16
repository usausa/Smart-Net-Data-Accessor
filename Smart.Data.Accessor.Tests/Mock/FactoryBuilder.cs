namespace Smart.Mock
{
    using System;

    using Smart.Data;
    using Smart.Data.Accessor;
    using Smart.Data.Accessor.Engine;

    public class FactoryBuilder
    {
        private readonly ExecuteEngineConfig config = new ExecuteEngineConfig();

        public FactoryBuilder EnableDebug()
        {
            return this;
        }

        public FactoryBuilder Config(Action<ExecuteEngineConfig> action)
        {
            action(config);
            return this;
        }

        public FactoryBuilder UseFileDatabase()
        {
            config.ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(TestDatabase.CreateConnection));
            });
            return this;
        }

        public FactoryBuilder UseMultipleDatabase()
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

        public FactoryBuilder UseMemoryDatabase()
        {
            config.ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(TestDatabase.CreateMemory));
            });
            return this;
        }

        public DataAccessorFactory Build()
        {
            return new DataAccessorFactory(config.ToEngine());
        }
    }
}
