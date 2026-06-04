namespace Smart.Data.Accessor.Tests;

using System;

using Smart.Data;
using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

// DataAccessorFactory / DataAccessorFactoryBuilder（DI コンテナ非使用の手動構築経路）。
// Pattern B アクセサの IDbProvider 解決、[Inject] シングルトン注入、GetService、型キャッシュ、Build 検証。
public sealed class FactoryTest
{
    private static DelegateDbProvider Provider() =>
        new(static () =>
        {
            var con = new MockDbConnection();
            con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataKind.Small },
                new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataKind.Large })));
            return con;
        });

    [Fact]
    public void CreatePatternBAccessorResolvesProviderAndExecutes()
    {
        var factory = new DataAccessorFactoryBuilder().UseDbProvider(Provider()).Build();

        var accessor = factory.Create<ProviderAccessor>();
        var list = accessor.QueryAll();

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
    }

    [Fact]
    public void CreateCachesAccessorByType()
    {
        var factory = new DataAccessorFactoryBuilder().UseDbProvider(Provider()).Build();

        var first = factory.Create<ProviderAccessor>();
        var second = factory.Create<ProviderAccessor>();

        Assert.Same(first, second);
    }

    [Fact]
    public void GetServiceReturnsProviderSingletonsAndNullForUnknown()
    {
        var provider = Provider();
        var counter = new Counter();
        var factory = new DataAccessorFactoryBuilder()
            .UseDbProvider(provider)
            .AddSingleton<ICounter>(counter)
            .Build();

        Assert.Same(provider, factory.GetService(typeof(IDbProvider)));
        Assert.Same(counter, factory.GetService(typeof(ICounter)));
        Assert.Null(factory.GetService(typeof(IDbProviderSelector)));
        Assert.Null(factory.GetService(typeof(string)));
    }

    [Fact]
    public void AddSingletonInjectedIntoAccessorConstructor()
    {
        var factory = new DataAccessorFactoryBuilder()
            .UseDbProvider(Provider())
            .AddSingleton<ICounter>(new Counter())
            .Build();

        var accessor = factory.Create<InjectAccessor>();

        Assert.Equal(1, accessor.UseInjected());
        Assert.Equal(2, accessor.UseInjected());
    }

    [Fact]
    public void BuildWithoutProviderThrows()
    {
        Assert.Throws<InvalidOperationException>(static () => new DataAccessorFactoryBuilder().Build());
    }

    [Fact]
    public void UseDbProviderSelectorExposedViaGetService()
    {
        var selector = new FixedSelector(Provider());
        var factory = new DataAccessorFactoryBuilder().UseDbProviderSelector(selector).Build();

        Assert.Same(selector, factory.GetService(typeof(IDbProviderSelector)));
        Assert.Null(factory.GetService(typeof(IDbProvider)));
    }

    private sealed class Counter : ICounter
    {
        private int n;

        public int Next() => ++n;
    }

    private sealed class FixedSelector : IDbProviderSelector
    {
        private readonly IDbProvider provider;

        public FixedSelector(IDbProvider provider) => this.provider = provider;

        public IDbProvider GetProvider(object parameter) => provider;
    }
}
