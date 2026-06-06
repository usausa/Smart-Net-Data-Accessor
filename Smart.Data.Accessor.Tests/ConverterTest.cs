namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Mock.Data;

using Xunit;

public sealed class ConverterTest
{
    [Fact]
    public void TypeHandlerConvertsTicksToDateTimeOnRead()
    {
        var expected = new DateTime(2026, 6, 1, 12, 30, 0, DateTimeKind.Utc);

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(long), "CreatedAt")
        };
        var rows = new List<object[]> { new object[] { 1L, expected.Ticks } };

        using var con = new MockDbConnection();
        con.SetupCommand(cmd => cmd.SetupResult(new MockDataReader(columns, rows)));

        var accessor = new ConverterAccessor();
        var list = accessor.QueryAll(con);

        var entity = Assert.Single(list);
        Assert.Equal(1L, entity.Id);
        Assert.Equal(expected, entity.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, entity.CreatedAt.Kind);
    }
}
