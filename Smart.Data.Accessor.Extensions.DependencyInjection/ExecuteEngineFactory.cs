namespace Smart
{
    using System;

    using Smart.Data;
    using Smart.Data.Accessor.Engine;

    public class ExecuteEngineFactory
    {
        private readonly ExecuteEngineConfig config;

        public ExecuteEngineFactory(ExecuteEngineConfig config)
            : this(config, null)
        {
        }

        public ExecuteEngineFactory(ExecuteEngineConfig config, IDbProvider provider)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (provider != null)
            {
                config.ConfigureComponents(components =>
                {
                    components.Add(provider);
                });
            }

            this.config = config;
        }

        public ExecuteEngine Create() => config.ToEngine();
    }
}
