namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

public sealed class QueryTest
{
    [Fact]
    public void QueryAllMapsAllRows()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static x => Assert.Equal("SELECT Id, Name, Type, Kind FROM Data ORDER BY Id", x.CommandText);
            cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
                new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large }));
        });

        var accessor = new QueryAccessor();
        var list = accessor.QueryAll(con);

        Assert.Equal(2, list.Count);
        Assert.Equal(1, list[0].Id);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(DataType.Large, list[1].Kind);
    }

    [Fact]
    public void QueryAllWithEmptyReaderReturnsEmptyList()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.EmptyDataReader()));

        var accessor = new QueryAccessor();
        var list = accessor.QueryAll(con);

        Assert.Empty(list);
    }

    [Fact]
    public void QueryFirstReturnsFirstRow()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 7, Name = "Carol", Type = 3, Kind = DataType.Small })));

        var accessor = new QueryAccessor();
        var entity = accessor.QueryFirst(con);

        Assert.NotNull(entity);
        Assert.Equal(7, entity.Id);
        Assert.Equal("Carol", entity.Name);
    }

    [Fact]
    public void QueryFirstWithEmptyReaderReturnsNull()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.EmptyDataReader()));

        var accessor = new QueryAccessor();
        var entity = accessor.QueryFirst(con);

        Assert.Null(entity);
    }
}
