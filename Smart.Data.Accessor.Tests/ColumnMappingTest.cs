namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

// 列マッピング戦略（PropertyGuard）の意味論を検証する：序数は名前照合（完全一致優先＋大小無視フォールバック）で解決し、
// 結果セットに無い列は「設定しない」（プロパティ初期化子が保持される）。record は全 ctor 引数必須のため欠落列は default。
public sealed class ColumnMappingTest
{
    private static List<object[]> Rows(params object[][] rows) => [.. rows];

    [Fact]
    public void SubsetColumnsLeaveUnmappedPropertiesUntouched()
    {
        // 3 プロパティ（Id / Name / Age）に対して Id 列だけ返す。Name / Age は初期化子の値が保持される。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([1L], [2L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryEntities(con);

        Assert.Equal(2, list.Count);
        Assert.Equal(1L, list[0].Id);
        Assert.Equal("unset", list[0].Name);
        Assert.Equal(-1, list[0].Age);
    }

    [Fact]
    public void ReorderedColumnsMapByName()
    {
        // SELECT 順がプロパティ宣言順と逆でも名前で対応付く。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(int), "Age"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(long), "Id")
            ],
            Rows([20, "Alice", 1L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryEntities(con);

        Assert.Single(list);
        Assert.Equal(1L, list[0].Id);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(20, list[0].Age);
    }

    [Fact]
    public void CaseInsensitiveColumnNamesMap()
    {
        // 列名の大小が異なっても照合される（GetOrdinal 相当の大小無視フォールバック）。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(long), "ID"),
                new MockColumn(typeof(string), "NAME")
            ],
            Rows([1L, "Alice"]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryEntities(con);

        Assert.Single(list);
        Assert.Equal(1L, list[0].Id);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(-1, list[0].Age);
    }

    [Fact]
    public void ExtraColumnsAreIgnored()
    {
        // エンティティに無い列は読み飛ばす。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Extra"),
                new MockColumn(typeof(string), "Name")
            ],
            Rows([1L, "x", "Alice"]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryEntities(con);

        Assert.Single(list);
        Assert.Equal(1L, list[0].Id);
        Assert.Equal("Alice", list[0].Name);
    }

    [Fact]
    public void RecordMissingColumnsReceiveDefaults()
    {
        // record 主コンストラクタは全引数必須のため、欠落列の引数は default になる（string は null）。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([7L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryRecords(con);

        Assert.Single(list);
        Assert.Equal(7L, list[0].Id);
        Assert.Null(list[0].Name);
        Assert.Equal(0, list[0].Age);
    }
}
