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
    public void DirectSqlQueryMapsRows()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("SELECT Id, Name, Type, Kind FROM Data", c.CommandText);
            cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
                new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large }));
        });

        var accessor = new DirectSqlAccessor();
        var list = accessor.QueryRaw(con, "SELECT Id, Name, Type, Kind FROM Data");

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(DataType.Large, list[1].Kind);
    }

    [Fact]
    public void DirectSqlExecuteReaderReturnsReader()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 7, Name = "Zoe", Type = 9, Kind = DataType.Small })));

        var accessor = new DirectSqlAccessor();
        using var reader = accessor.ReadRaw(con, "SELECT Id, Name, Type, Kind FROM Data");

        Assert.True(reader.Read());
        Assert.Equal("Zoe", reader.GetString(reader.GetOrdinal("Name")));
    }

    [Fact]
    public void PatternBResolvesConnectionFromDbProvider()
    {
        var provider = new DelegateDbProvider(static () =>
        {
            var c = new MockDbConnection();
            c.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
                new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large })));
            return c;
        });

        var accessor = new ProviderAccessor(provider);
        var list = accessor.QueryAll();

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
    }
}
