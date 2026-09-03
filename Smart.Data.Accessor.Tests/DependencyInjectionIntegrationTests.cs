namespace Smart.Data.Accessor.Tests;

using Microsoft.Extensions.DependencyInjection;

using Smart.Data;
using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

// M.E.DI 統合の検証：AddDataAccessors() が生成登録済みアクセサを束縛し、Pattern B アクセサが
// コンテナから IDbProvider を解決して実行できる。Assembly 指定オーバーロードは、アクセサ別
// アセンブリ構成でモジュール初期化子を先行実行するためのもの(同一アセンブリでは no-op で同結果。
// 別アセンブリでの罠と解消の実機検証はマルチアセンブリ構成が必要なためテンポラリ project で実施)。
public sealed class DependencyInjectionIntegrationTests
{
    private static DelegateDbProvider CreateProvider() => new(static () =>
    {
        var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
            new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large })));
        return con;
    });

    [Fact]
    public void AddDataAccessorsResolvesPatternBAccessorAndExecutes()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDbProvider>(CreateProvider());
        services.AddDataAccessors();

        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<ProviderAccessor>();
        var list = accessor.QueryAll();

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
    }

    [Fact]
    public void AddDataAccessorsWithAssembliesRunsInitializersFirst()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDbProvider>(CreateProvider());
        services.AddDataAccessors(typeof(ProviderAccessor).Assembly);

        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<ProviderAccessor>();
        var list = accessor.QueryAll();

        Assert.Equal(2, list.Count);
        Assert.Equal("Bob", list[1].Name);
    }
}
