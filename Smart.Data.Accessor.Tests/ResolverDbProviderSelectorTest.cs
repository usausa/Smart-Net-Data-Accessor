namespace Smart.Data.Accessor.Tests;

using System;

using Smart.Data;
using Smart.Data.Accessor.Resolver;
using Smart.Mock.Data;
using Smart.Resolver;

using Xunit;

// ResolverDbProviderSelector を直接検証する。Pattern B + [Provider("name")] の選択経路では生成コードが
// providerSelector.GetProvider("name") を呼び、これが Smart.Resolver の keyed IDbProvider を解決する。
// 既存 ResolverIntegrationTest は [Provider] 無しの ProviderAccessor なのでこの経路を通らない。
public sealed class ResolverDbProviderSelectorTest
{
    [Fact]
    public void GetProviderResolvesKeyedProviderByName()
    {
        var provider = new DelegateDbProvider(static () => new MockDbConnection());

        var config = new ResolverConfig();
        config.Bind<IDbProvider>().ToConstant(provider).Keyed("main");
        using var resolver = config.ToResolver();

        var selector = new ResolverDbProviderSelector(resolver);

        Assert.Same(provider, selector.GetProvider("main"));
    }

    [Fact]
    public void ConstructorThrowsOnNullResolver() =>
        Assert.Throws<ArgumentNullException>(static () => new ResolverDbProviderSelector(null!));

    [Fact]
    public void GetProviderThrowsOnNullParameter()
    {
        using var resolver = new ResolverConfig().ToResolver();
        var selector = new ResolverDbProviderSelector(resolver);

        Assert.Throws<ArgumentNullException>(() => selector.GetProvider(null!));
    }
}
