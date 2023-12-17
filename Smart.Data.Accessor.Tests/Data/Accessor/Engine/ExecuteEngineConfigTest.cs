namespace Smart.Data.Accessor.Engine;

using Smart.ComponentModel;

public sealed class ExecuteEngineConfigTest
{
    [Fact]
    public void CoverageFix()
    {
        var config = new ExecuteEngineConfig();

        var provider = new ComponentConfig().ToContainer();
        config.SetServiceProvider(provider);
        Assert.Equal(provider, ((IExecuteEngineConfig)config).GetServiceProvider());

        config.ConfigureComponents(_ => { });
        Assert.NotEqual(provider, ((IExecuteEngineConfig)config).GetServiceProvider());
    }
}
