namespace Smart.Mock
{
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

        public GeneratorBuilder UseFileDatabase()
        {
            config.ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(TestDatabase.CreateConnection));
            });
            return this;
        }

        public GeneratorBuilder SetSql(string sql)
        {
            loader = new ConstLoader(sql);
            return this;
        }

        // TODO simple loader

        public DaoGenerator Build()
        {
            return new DaoGenerator(config.ToEngine(), loader, debug ? new SqlDebugger() : null);
        }
    }
}
