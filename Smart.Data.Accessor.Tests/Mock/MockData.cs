namespace Smart.Data.Accessor.Tests.Mock;

using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

// Builds Smart.Mock.Data readers for the test entity shape.
internal static class MockData
{
    private static readonly MockColumn[] DataColumns =
    [
        new(typeof(long), "Id"),
        new(typeof(string), "Name"),
        new(typeof(int), "Type"),
        new(typeof(int), "Kind")
    ];

    public static MockDataReader DataReader(params DataEntity[] rows) =>
        new(DataColumns, rows.Select(static x => new object[] { x.Id, x.Name, x.Type, (int)x.Kind }).ToList());

    public static MockDataReader EmptyDataReader() =>
        new(DataColumns, new List<object[]>());
}
