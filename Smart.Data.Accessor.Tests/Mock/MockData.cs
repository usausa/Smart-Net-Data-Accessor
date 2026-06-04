namespace Smart.Data.Accessor.Tests.Mock;

using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

// Builds Smart.Mock.Data readers for the test entity shape.
internal static class MockData
{
    private static readonly MockColumn[] DataColumns =
    [
        new MockColumn(typeof(long), "Id"),
        new MockColumn(typeof(string), "Name"),
        new MockColumn(typeof(int), "Type"),
        new MockColumn(typeof(int), "Kind"),
    ];

    public static MockDataReader DataReader(params DataEntity[] rows) =>
        new(DataColumns, rows.Select(static e => new object[] { e.Id, e.Name, e.Type, (int)e.Kind }).ToList());

    public static MockDataReader EmptyDataReader() =>
        new(DataColumns, new List<object[]>());
}
