namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

public sealed class DirectSqlAndProviderTest
{
    [Fact]
    public void DirectSqlUsesParameterAsCommandText()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("DELETE FROM Data WHERE Type = 1", c.CommandText);
            cmd.SetupResult(2);
        });

        var accessor = new DirectSqlAccessor();
        var affected = accessor.ExecRaw(con, "DELETE FROM Data WHERE Type = 1");

        Assert.Equal(2, affected);
    }

    [Fact]
    public void PatternBResolvesConnectionFromDbProvider()
    {
        var provider = new DelegateDbProvider(static () =>
        {
            var c = new MockDbConnection();
            c.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataKind.Small },
                new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataKind.Large })));
            return c;
        });

        var accessor = new ProviderAccessor(provider);
        var list = accessor.QueryAll();

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
    }
}
