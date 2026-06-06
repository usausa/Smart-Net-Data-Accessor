namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Mock;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

public sealed class DynamicSqlTest
{
    [Fact]
    public void ConditionalBlockOmittedWhenArgumentIsNull()
    {
        string? captured = null;
        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => captured = c.CommandText;
            cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "Alice", Type = 1, Kind = DataType.Small }));
        });

        var accessor = new DynamicAccessor();
        accessor.QueryByOptionalId(con, null);

        Assert.NotNull(captured);
        Assert.DoesNotContain("WHERE", captured!, StringComparison.Ordinal);
    }

    [Fact]
    public void ConditionalBlockEmittedWhenArgumentPresent()
    {
        string? captured = null;
        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => captured = c.CommandText;
            cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 5, Name = "Bob", Type = 2, Kind = DataType.Large }));
        });

        var accessor = new DynamicAccessor();
        accessor.QueryByOptionalId(con, 5);

        Assert.NotNull(captured);
        Assert.Contains("WHERE Id >= @p0", captured!, StringComparison.Ordinal);
    }

    [Fact]
    public void InClauseExpandsToOneParameterPerElement()
    {
        string? captured = null;
        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => captured = c.CommandText;
            cmd.SetupResult(MockData.DataReader(
                new DataEntity { Id = 1, Name = "A", Type = 1, Kind = DataType.Small },
                new DataEntity { Id = 2, Name = "B", Type = 2, Kind = DataType.Large }));
        });

        var accessor = new DynamicAccessor();
        accessor.QueryByIds(con, [1L, 2L, 3L]);

        Assert.NotNull(captured);
        Assert.Contains("@p0_0", captured!, StringComparison.Ordinal);
        Assert.Contains("@p0_2", captured!, StringComparison.Ordinal);
    }

    [Fact]
    public void InClauseEmptyCollectionExpandsToNull()
    {
        string? captured = null;
        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => captured = c.CommandText;
            cmd.SetupResult(MockData.EmptyDataReader());
        });

        var accessor = new DynamicAccessor();
        accessor.QueryByIds(con, Array.Empty<long>());

        Assert.NotNull(captured);
        Assert.Contains("(NULL)", captured!, StringComparison.Ordinal);
    }
}
