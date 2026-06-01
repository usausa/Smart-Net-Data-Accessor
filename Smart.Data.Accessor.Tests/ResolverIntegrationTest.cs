namespace Smart.Data.Accessor.Tests;

using Smart.Data;
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
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataKind.Small },
                new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataKind.Large })));
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
}
