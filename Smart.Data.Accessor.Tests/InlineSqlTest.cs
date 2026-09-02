namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Mock.Data;

using Xunit;

// [Sql](インライン 2-way SQL)の実行時挙動を検証する：静的 SQL の CommandText 直埋め(raw string literal の
// 改行は空白 1 個に正規化)、/*% */ 条件分岐、/*@ */ バインド。SQL ファイルは一切使わない。
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local
public sealed class InlineSqlTest
{
    private static List<object[]> Rows(params object[][] rows) => [.. rows];

    private static MockDataReader CreateReader(params object[][] rows) => new(
        [
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(string), "Name"),
            new MockColumn(typeof(int), "Age")
        ],
        Rows(rows));

    [Fact]
    public void StaticInlineSqlExecutesAndMaps()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static x => Assert.Equal("SELECT Id, Name, Age FROM Data ORDER BY Id", x.CommandText);
            cmd.SetupResult(CreateReader([1L, "Alice", 20], [2L, "Bob", 30]));
        });

        var accessor = new InlineSqlAccessor();
        var list = accessor.QueryAll(con);

        Assert.Equal(2, list.Count);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(30, list[1].Age);
    }

    [Fact]
    public void DynamicInlineSqlSkipsBranchWhenConditionFalse()
    {
        // minAge = 0 → /*% if */ ブロックが落ち、WHERE 句もパラメータも出ない。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static x =>
            {
                Assert.DoesNotContain("WHERE", x.CommandText, StringComparison.Ordinal);
                Assert.Equal(0, x.Parameters.Count);
            };
            cmd.SetupResult(CreateReader([1L, "Alice", 20]));
        });

        var accessor = new InlineSqlAccessor();
        var list = accessor.QueryByAge(con, 0);

        Assert.Single(list);
    }

    [Fact]
    public void DynamicInlineSqlAppliesBranchWhenConditionTrue()
    {
        // minAge > 0 → WHERE 句が組まれ、/*@ minAge */ が 1 個バインドされる。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static x =>
            {
                Assert.Contains("WHERE Age >=", x.CommandText, StringComparison.Ordinal);
                Assert.Equal(1, x.Parameters.Count);
                Assert.Equal(25, x.Parameters[0].Value);
            };
            cmd.SetupResult(CreateReader([2L, "Bob", 30]));
        });

        var accessor = new InlineSqlAccessor();
        var list = accessor.QueryByAge(con, 25);

        Assert.Single(list);
        Assert.Equal("Bob", list[0].Name);
    }

    [Fact]
    public void StaticInlineSqlBindsParameters()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static x =>
            {
                Assert.StartsWith("UPDATE Data SET Name = ", x.CommandText, StringComparison.Ordinal);
                Assert.Equal(2, x.Parameters.Count);
            };
            cmd.SetupResult(1);
        });

        var accessor = new InlineSqlAccessor();
        var affected = accessor.UpdateName(con, 5L, "Carol");

        Assert.Equal(1, affected);
    }
}
