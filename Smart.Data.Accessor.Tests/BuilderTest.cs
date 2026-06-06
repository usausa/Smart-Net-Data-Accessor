namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

public sealed class BuilderTest
{
    [Fact]
    public void InsertBuildsInsertStatementExcludingDatabaseManagedKey()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal(
                "INSERT INTO \"Data\" (\"Name\", \"Type\", \"Kind\") VALUES (@Name, @Type, @Kind)", c.CommandText);
            cmd.SetupResult(1);
        });

        var accessor = new BuilderAccessor();
        var affected = accessor.Insert(con, new DataEntity { Name = "Alice", Type = 1, Kind = DataType.Small });

        Assert.Equal(1, affected);
    }

    [Fact]
    public void UpdateBuildsUpdateStatementWithKeyWhere()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal(
                "UPDATE \"Data\" SET \"Name\" = @Name, \"Type\" = @Type, \"Kind\" = @Kind WHERE \"Id\" = @k_Id", c.CommandText);
            cmd.SetupResult(1);
        });

        var accessor = new BuilderAccessor();
        var affected = accessor.Update(con, new DataEntity { Id = 9, Name = "Bob", Type = 2, Kind = DataType.Large });

        Assert.Equal(1, affected);
    }

    [Fact]
    public void DeleteBuildsDeleteStatementWithKeyWhere()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("DELETE FROM \"Data\" WHERE \"Id\" = @id", c.CommandText);
            cmd.SetupResult(1);
        });

        var accessor = new BuilderAccessor();
        var affected = accessor.DeleteById(con, 9);

        Assert.Equal(1, affected);
    }

    [Fact]
    public void CountBuildsCountStatement()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("SELECT COUNT(*) FROM \"Data\"", c.CommandText);
            cmd.SetupResult(3L);
        });

        var accessor = new BuilderAccessor();
        Assert.Equal(3L, accessor.CountAll(con));
    }

    [Fact]
    public void SelectBuildsSelectStatementAndMaps()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal(
                "SELECT \"Id\", \"Name\", \"Type\", \"Kind\" FROM \"Data\"", c.CommandText);
            cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small }));
        });

        var accessor = new BuilderAccessor();
        var list = accessor.SelectAll(con);

        Assert.Single(list);
        Assert.Equal("Alice", list[0].Name);
    }

    [Fact]
    public void SelectSingleBuildsWhereAndReturnsFirst()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal(
                "SELECT \"Id\", \"Name\", \"Type\", \"Kind\" FROM \"Data\" WHERE \"Id\" = @id", c.CommandText);
            cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 9, Name = "Bob", Type = 2, Kind = DataType.Large }));
        });

        var accessor = new BuilderAccessor();
        var entity = accessor.Find(con, 9);

        Assert.NotNull(entity);
        Assert.Equal(9, entity!.Id);
    }

    [Fact]
    public void InsertParameterModeUsesMethodParametersAsColumns()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal(
                "INSERT INTO \"Data\" (\"id\", \"name\") VALUES (@id, @name)", c.CommandText);
            cmd.SetupResult(1);
        });

        var accessor = new BuilderAccessor();
        var affected = accessor.InsertRaw(con, 1, "Alice");

        Assert.Equal(1, affected);
    }
}
