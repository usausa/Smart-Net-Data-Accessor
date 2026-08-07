namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

// [Naming(SnakeCaseLower)] のエンドツーエンド実行検証。Builder の生成 SQL はテーブル名/列名が snake_case に
// なり(バインドパラメータ名はプロパティ名のまま)、Query の結果マッピングは snake_case 列と照合される。
// End-to-end runtime coverage for [Naming(SnakeCaseLower)]: the builder SQL carries snake_case table /
// column names (bind parameter names stay property-based), and the Query result mapping matches
// snake_case columns.
public sealed class NamingTest
{
    [Fact]
    public void InsertBuildsSnakeCaseStatement()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static x => Assert.Equal(
                "INSERT INTO \"naming_entity\" (\"user_id\", \"first_name\") VALUES (@UserId, @FirstName)", x.CommandText);
            cmd.SetupResult(1);
        });

        var accessor = new NamingAccessor();
        var affected = accessor.Insert(con, new NamingEntity { UserId = 1, FirstName = "Alice" });

        Assert.Equal(1, affected);
    }

    [Fact]
    public void QueryMapsSnakeCaseColumns()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(long), "user_id"),
                new MockColumn(typeof(string), "first_name")
            ],
            [[1L, "Alice"]])));

        var accessor = new NamingAccessor();
        var list = accessor.QueryEntities(con);

        Assert.Single(list);
        Assert.Equal(1L, list[0].UserId);
        Assert.Equal("Alice", list[0].FirstName);
    }
}
