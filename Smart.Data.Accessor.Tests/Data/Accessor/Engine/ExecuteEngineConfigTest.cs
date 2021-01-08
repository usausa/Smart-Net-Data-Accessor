namespace Smart.Data.Accessor.Engine
{
    using System;

    using Smart.ComponentModel;

    using Xunit;

    public class ExecuteEngineConfigTest
    {
        [Fact]
        public void CoverageFix()
        {
            var config = new ExecuteEngineConfig();

            Assert.Throws<ArgumentNullException>(() => config.SetServiceProvider(null));
            Assert.Throws<ArgumentNullException>(() => config.ConfigureComponents(null));
            Assert.Throws<ArgumentNullException>(() => config.ConfigureTypeMap(null));
            Assert.Throws<ArgumentNullException>(() => config.ConfigureTypeHandlers(null));
            Assert.Throws<ArgumentNullException>(() => config.ConfigureResultMapperFactories(null));

            var provider = new ComponentConfig().ToContainer();
            config.SetServiceProvider(provider);
            Assert.Equal(provider, ((IExecuteEngineConfig)config).GetServiceProvider());

            config.ConfigureComponents(_ => { });
            Assert.NotEqual(provider, ((IExecuteEngineConfig)config).GetServiceProvider());
        }
    }
}
