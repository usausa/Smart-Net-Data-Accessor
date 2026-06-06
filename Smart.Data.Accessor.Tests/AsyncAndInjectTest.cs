namespace Smart.Data.Accessor.Tests;

using System.Data;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

public sealed class AsyncAndInjectTest
{
    [Fact]
    public async Task QueryAllAsyncMapsRows()
    {
        await using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.DataReader(
            new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small },
            new DataEntity { Id = 2, Name = "Bob", Type = 2, Kind = DataType.Large })));

        var accessor = new AsyncAccessor();
        var list = await accessor.QueryAllAsync(con, CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.Equal("Bob", list[1].Name);
    }

    [Fact]
    public async Task InsertAsyncReturnsAffected()
    {
        await using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(1));

        var accessor = new AsyncAccessor();
        var affected = await accessor.InsertAsync(con, "Alice", 1);

        Assert.Equal(1, affected);
    }

    [Fact]
    public void TransactionArgumentUsesItsConnection()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("INSERT INTO Data (Name, Type) VALUES (@p0, @p1)", c.CommandText);
            cmd.SetupResult(1);
        });
        con.Open();

        using var tx = con.BeginTransaction();
        var accessor = new MiscAccessor();
        var affected = accessor.InsertByTx(tx, "Alice", 1);

        Assert.Equal(1, affected);
    }

    [Fact]
    public void DbTypeAttributeIsAppliedToParameter()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal("INSERT INTO Data (Name) VALUES (@p0)", c.CommandText);
                var p = (MockDbParameter)c.Parameters[0];
                Assert.Equal(DbType.AnsiString, p.DbType);
            };
            cmd.SetupResult(1);
        });

        var accessor = new MiscAccessor();
        Assert.Equal(1, accessor.InsertAnsi(con, "Alice"));
    }

    [Fact]
    public void InjectConstructorReceivesService()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(MockData.EmptyDataReader()));

        var accessor = new InjectAccessor(new StepCounter());

        Assert.Empty(accessor.QueryAll(con));
        Assert.Equal(1, accessor.UseInjected());
        Assert.Equal(2, accessor.UseInjected());
    }

    private sealed class StepCounter : ICounter
    {
        private int count;

        public int Next() => ++count;
    }
}
