namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Data.Accessor.Tests.Models;
using Smart.Mock.Data;

using Xunit;

// 列マッピング戦略(PropertyGuard)の意味論を検証する：序数は事前構築の FrozenDictionary(OrdinalIgnoreCase)による
// 大小無視・先勝ち照合で解決し、結果セットに無い列は「設定しない」(プロパティ初期化子が保持される)。
// record は全 ctor 引数必須のため欠落列は default(引数型)。
public sealed class ColumnMappingTest
{
    private static List<object[]> Rows(params object[][] rows) => [.. rows];

    [Fact]
    public void SubsetColumnsLeaveUnmappedPropertiesUntouched()
    {
        // 3 プロパティ(Id / Name / Age)に対して Id 列だけ返す。Name / Age は初期化子の値が保持される。
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
        // 列名の大小が異なっても照合される(FrozenDictionary は OrdinalIgnoreCase・先勝ち)。
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
    public void InitOnlyPropertiesReceiveDefaultWhenColumnMissing()
    {
        // init-only は `new T { ... }` 内でしか設定できないため、欠落列は default になる(初期化子 "unset" は保持されない)。
        // settable な Age は従来どおり未設定＝初期値保持。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([5L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryInitEntities(con);

        Assert.Single(list);
        Assert.Equal(5L, list[0].Id);
        Assert.Null(list[0].Name);
        Assert.Equal(-1, list[0].Age);
    }

    [Fact]
    public void OverloadedQueryMethodsShareMapping()
    {
        // 同名オーバーロード(同一エンティティ・同一列リスト)が共有 struct / マッパーで動作する。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(int), "Age")
            ],
            Rows([1L, "Alice", 20]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryEntities(con, 20);

        Assert.Single(list);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(20, list[0].Age);
    }

    [Fact]
    public void RecordIgnoredParameterReceivesDefault()
    {
        // [property: Ignore] の位置引数はマップされず default(null)が渡る。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([3L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryIgnoredRecords(con);

        Assert.Single(list);
        Assert.Equal(3L, list[0].Id);
        Assert.Null(list[0].Temp);
    }

    [Fact]
    public void RecordMissingColumnsReceiveDefaults()
    {
        // record 主コンストラクタは全引数必須のため、欠落列の引数は default になる(string は null)。
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

    [Fact]
    public void DbNullMapsToNullForNullableColumns()
    {
        // DB NULL は Nullable 値型・Nullable enum・参照型のいずれも null になる。生成コードの default は
        // プロパティ型で型付けされる(素の default! だと三項式の自然型が int に決まり 0 が入ってしまう)。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(int), "Age"),
                new MockColumn(typeof(int), "Kind"),
                new MockColumn(typeof(string), "Note")
            ],
            Rows([1L, DBNull.Value, DBNull.Value, DBNull.Value], [2L, 30, 1, "memo"]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryNullableEntities(con);

        Assert.Equal(2, list.Count);
        Assert.Equal(1L, list[0].Id);
        Assert.Null(list[0].Age);
        Assert.Null(list[0].Kind);
        Assert.Null(list[0].Note);
        Assert.Equal(2L, list[1].Id);
        Assert.Equal(30, list[1].Age);
        Assert.Equal(MappingKind.First, list[1].Kind);
        Assert.Equal("memo", list[1].Note);
    }

    [Fact]
    public void RecordMissingNullableColumnsReceiveNull()
    {
        // record 主 ctor の欠落列ガードも引数型で型付けされるため、int?/enum? の欠落列は 0 ではなく null が渡る。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([9L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryNullableRecords(con);

        Assert.Single(list);
        Assert.Equal(9L, list[0].Id);
        Assert.Null(list[0].Age);
        Assert.Null(list[0].Kind);
    }

    [Fact]
    public void RequiredUnmappedMembersReceiveDefault()
    {
        // マップ対象外の required メンバ([Ignore] 付き・非 public)は初期化子で default! が設定される
        // (設定しないと生成コードが CS9035 でコンパイル不能。このテストのコンパイル自体が回避の証明)。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([1L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryRequiredEntities(con);

        Assert.Single(list);
        Assert.Equal(1L, list[0].Id);
        Assert.Null(list[0].Secret);
        Assert.Null(list[0].Hidden);
    }

    [Fact]
    public void RecordNonPositionalRequiredMemberReceivesDefault()
    {
        // record 主 ctor 外(非位置)の required メンバは ctor 呼び出し後の初期化子で default! が設定される。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([2L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryRequiredRecords(con);

        Assert.Single(list);
        Assert.Equal(2L, list[0].Id);
        Assert.Null(list[0].Name);
    }

    [Fact]
    public void RecordIgnoredParameterKeepsDeclaredDefault()
    {
        // 宣言既定値を持つ [property: Ignore] 引数は名前付き引数ごと省略され、宣言既定値("db")が生きる
        // (default! を渡すと null に上書きされてしまう)。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([3L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryDefaultRecords(con);

        Assert.Single(list);
        Assert.Equal(3L, list[0].Id);
        Assert.Equal("db", list[0].Source);
    }

    [Fact]
    public void DerivedNameCollisionEntitiesBothMap()
    {
        // MapCollide の struct 名と CollideOrdinals のマッパー名が交差衝突するペア。連番で一意化され、
        // 両方のアクセサが動作する(テストプロジェクトがコンパイルできること自体が CS0102 回避の証明)。
        // 2 本目は大小違いの列名("ID")で、narrow エンティティの直比較 __From も大小無視で照合することを確認する。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "Id")],
            Rows([4L]))));

        var accessor = new MappingAccessor();
        var first = accessor.QueryMapCollides(con);

        using var con2 = new MockDbConnection();
        con2.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [new MockColumn(typeof(long), "ID")],
            Rows([5L]))));
        var second = accessor.QueryCollideOrdinals(con2);

        Assert.Equal(4L, first[0].Id);
        Assert.Equal(5L, second[0].Id);
    }
}
