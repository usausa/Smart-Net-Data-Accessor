namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Resolver;
using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;
using Smart.Resolver;

using Xunit;

// Verifies the Smart.Resolver integration: UseDataAccessors() binds the generator-registered
// accessors, and a Pattern B accessor resolves its IDbProvider from the container.
public sealed class ResolverIntegrationTest
{
    [Fact]
    public void ResolvesPatternBAccessorAndExecutes()
    {
        var provider = new DelegateDbProvider(static () =>
        {
            var con = new MockDbConnection();
            con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
                new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large })));
            return con;
        });

        var config = new ResolverConfig();
        config.UseDataAccessors();
        config.Bind<IDbProvider>().ToConstant(provider).InSingletonScope();

        using var resolver = config.ToResolver();
        var accessor = resolver.Get<ProviderAccessor>();
        var list = accessor.QueryAll();

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal("Bob", list[1].Name);
    }

    [Fact]
    public void UseDataAccessorsWithAssembliesRunsInitializersFirst()
    {
        // Assembly 指定オーバーロード：アクセサ別アセンブリ構成でモジュール初期化子を先行実行してから登録する
        // (同一アセンブリでは no-op で同結果。別アセンブリでの罠と解消はテンポラリ project で実機検証)。
        var provider = new DelegateDbProvider(static () =>
        {
            var con = new MockDbConnection();
            con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 7, Name = "Carol", Type = 3, Kind = DataType.Small })));
            return con;
        });

        var config = new ResolverConfig();
        config.UseDataAccessors(typeof(ProviderAccessor).Assembly);
        config.Bind<IDbProvider>().ToConstant(provider).InSingletonScope();

        using var resolver = config.ToResolver();
        var accessor = resolver.Get<ProviderAccessor>();
        var list = accessor.QueryAll();

        Assert.Single(list);
        Assert.Equal("Carol", list[0].Name);
    }
}
