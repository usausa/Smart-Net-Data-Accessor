namespace Smart.Data.Accessor.Tests;

using System.Data;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

public sealed class ExecuteTest
{
    [Fact]
    public void InsertNameBuildsParameterizedSqlAndReturnsAffected()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal("INSERT INTO Data (Name, Type) VALUES (@p0, @p1)", c.CommandText);
                Assert.Equal(2, c.Parameters.Count);
            };
            cmd.SetupResult(1);
        });

        var accessor = new ExecuteAccessor();
        var affected = accessor.InsertName(con, "Alice", 1);

        Assert.Equal(1, affected);
    }

    [Fact]
    public void DeleteByIdVoidExecutes()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("DELETE FROM Data WHERE Id = @p0", c.CommandText);
            cmd.SetupResult(1);
        });

        var accessor = new ExecuteAccessor();
        accessor.DeleteById(con, 5);
    }

    [Fact]
    public void CountAllReturnsScalar()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("SELECT COUNT(*) FROM Data", c.CommandText);
            cmd.SetupResult(42L);
        });

        var accessor = new ExecuteAccessor();
        var count = accessor.CountAll(con);

        Assert.Equal(42L, count);
    }

    [Fact]
    public void ReadAllReturnsWrappedReader()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataKind.Small },
            new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataKind.Large })));

        var accessor = new ExecuteAccessor();
        var rows = 0;
        using (var reader = accessor.ReadAll(con))
        {
            while (reader.Read())
            {
                rows++;
            }
        }

        Assert.Equal(2, rows);
    }

    [Fact]
    public void QueryRecordsMapsViaRecordPrimaryConstructor()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataKind.Small },
            new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataKind.Large })));

        var accessor = new ExecuteAccessor();
        var list = accessor.QueryRecords(con);

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(DataKind.Large, list[1].Kind);
    }
}
