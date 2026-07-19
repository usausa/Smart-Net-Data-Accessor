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

    //--------------------------------------------------------------------------------
    // 閾値超え(ハッシュ switch 形)の序数解決
    // Above-threshold ordinal resolution (the hash-switch form)
    //--------------------------------------------------------------------------------

    [Fact]
    public void WideEntityResolvesEveryColumn()
    {
        // 全 17 列を宣言順で返す。switch 形が全グループを正しい序数へ束縛することの基本確認。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(int), "Age"),
                new MockColumn(typeof(string), "Email"),
                new MockColumn(typeof(string), "Phone"),
                new MockColumn(typeof(string), "City"),
                new MockColumn(typeof(string), "State"),
                new MockColumn(typeof(string), "Country"),
                new MockColumn(typeof(string), "Note"),
                new MockColumn(typeof(int), "Status"),
                new MockColumn(typeof(int), "Rank"),
                new MockColumn(typeof(int), "Level"),
                new MockColumn(typeof(int), "OwnerId"),
                new MockColumn(typeof(int), "GroupId"),
                new MockColumn(typeof(int), "Version"),
                new MockColumn(typeof(string), "CreatedBy"),
                new MockColumn(typeof(string), "UpdatedBy")
            ],
            Rows([1L, "Alice", 20, "a@example.com", "090", "Tokyo", "Tokyo", "JP", "note", 1, 2, 3, 4, 5, 6, "creator", "updater"]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryWideEntities(con);

        Assert.Single(list);
        Assert.Equal(1L, list[0].Id);
        Assert.Equal("Alice", list[0].Name);
        Assert.Equal(20, list[0].Age);
        Assert.Equal("JP", list[0].Country);
        Assert.Equal(6, list[0].Version);
        Assert.Equal("updater", list[0].UpdatedBy);
    }

    [Fact]
    public void WideEntityMapsReversedCaseVariantAndUnmappedColumns()
    {
        // 逆順・大小違い・未マップ列混在・一部欠落を同時に与える。switch 形は列順に依存せず名前で対応付き、
        // 未マップ列は default: で捨てられ、欠落列はプロパティ初期化子の値を保持する。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(string), "unmapped_one"),
                new MockColumn(typeof(string), "UPDATEDBY"),
                new MockColumn(typeof(int), "version"),
                new MockColumn(typeof(string), "unmapped_two"),
                new MockColumn(typeof(string), "country"),
                new MockColumn(typeof(int), "AGE"),
                new MockColumn(typeof(string), "nAmE"),
                new MockColumn(typeof(long), "id")
            ],
            Rows(["x", "updater", 9, "y", "JP", 30, "Bob", 7L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryWideEntities(con);

        Assert.Single(list);
        Assert.Equal(7L, list[0].Id);
        Assert.Equal("Bob", list[0].Name);
        Assert.Equal(30, list[0].Age);
        Assert.Equal("JP", list[0].Country);
        Assert.Equal(9, list[0].Version);
        Assert.Equal("updater", list[0].UpdatedBy);
        // 結果セットに無い列は設定されない。
        Assert.Equal("unset", list[0].Email);
        Assert.Equal(-1, list[0].Status);
    }

    [Fact]
    public void WideEntityToleratesEmptyColumnName()
    {
        // プロバイダは無名の式列に空の列名を返すことがある(SQL Server の別名なし `SELECT 1` など)。
        // ハッシュは name[0] を読むため、長さ 0 のガードが無いと IndexOutOfRangeException になる。
        // 置き換え前の FrozenDictionary 形は「一致なし」として無害に処理していたので、退行させてはいけない。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(int), string.Empty),
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(int), string.Empty)
            ],
            Rows([0, 11L, "Carol", 0]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryWideEntities(con);

        Assert.Single(list);
        Assert.Equal(11L, list[0].Id);
        Assert.Equal("Carol", list[0].Name);
    }

    [Fact]
    public void CollidingHashColumnNamesResolveIndependently()
    {
        // item_code / item_name / item_note / item_type / item_size は既定のサンプリング位置で同じハッシュに落ちる。
        // 同じ case に同居しても、case 内の String.Equals が各キーを取り違えずに解くことを確認する。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(string), "item_size"),
                new MockColumn(typeof(string), "item_note"),
                new MockColumn(typeof(string), "item_code"),
                new MockColumn(typeof(string), "item_type"),
                new MockColumn(typeof(string), "item_name"),
                new MockColumn(typeof(int), "flag_11"),
                new MockColumn(typeof(int), "flag_01"),
                new MockColumn(typeof(int), "value_03"),
                new MockColumn(typeof(int), "t2_value"),
                new MockColumn(typeof(int), "t1_value")
            ],
            Rows(["L", "memo", "C-1", "T", "Widget", 11, 1, 3, 22, 21]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryCollideHashEntities(con);

        Assert.Single(list);
        Assert.Equal("C-1", list[0].ItemCode);
        Assert.Equal("Widget", list[0].ItemName);
        Assert.Equal("memo", list[0].ItemNote);
        Assert.Equal("T", list[0].ItemType);
        Assert.Equal("L", list[0].ItemSize);
        Assert.Equal(1, list[0].Flag01);
        Assert.Equal(11, list[0].Flag11);
        Assert.Equal(3, list[0].Value03);
        // どの三つ組でも同じハッシュに落ちるペア。case 内の Equals 連鎖が取り違えずに解くこと。
        // The pair that collides under every triple: the in-case Equals chain must not mix them up.
        Assert.Equal(21, list[0].T1Value);
        Assert.Equal(22, list[0].T2Value);
        // 与えていない列は初期値のまま。
        Assert.Equal("unset", list[0].ItemUnit);
        Assert.Equal(-1, list[0].Flag02);
    }

    [Fact]
    public void MixedScriptColumnNamesResolveViaHashSwitch()
    {
        // ASCII/非 ASCII 混在キー(user_名前 / col_дата は非サンプリング位置にのみ非 ASCII を含む)でも switch 形が
        // 選ばれ、正しく解決されることの実行時検証。COL_ДАТА はキリル文字の大小違い(д↔Д 等は OrdinalIgnoreCase で
        // 等価)を含む＝「サンプリング位置が ASCII なら、大小違いの実行時文字列もハッシュが一致する」という
        // switch 形の健全性前提そのものを通す入力。
        // Runtime check that mixed keys (user_名前 / col_дата carry non-ASCII only at non-sampled positions) still take
        // the switch form and resolve. COL_ДАТА is a Cyrillic case variant (д↔Д are OrdinalIgnoreCase-equal), driving
        // exactly the soundness premise: ASCII sampled positions imply hash equality for case-variant runtime strings.
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(int), "STATUS"),
                new MockColumn(typeof(string), "USER_名前"),
                new MockColumn(typeof(string), "COL_ДАТА"),
                new MockColumn(typeof(string), "unmapped_extra"),
                new MockColumn(typeof(long), "id")
            ],
            Rows([5, "山田", "2026-01-01", "x", 9L]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryMixedNameEntities(con);

        Assert.Single(list);
        Assert.Equal(5, list[0].Status);
        Assert.Equal("山田", list[0].UserName);
        Assert.Equal("2026-01-01", list[0].ColDate);
        Assert.Equal(9L, list[0].Id);
        // 与えていない列は初期値のまま。
        Assert.Equal("unset", list[0].City);
        Assert.Equal(-1, list[0].Age);
    }

    [Fact]
    public void DuplicateColumnNamesFirstMatchWins()
    {
        // UNION や JOIN の SELECT * では同名列が重複し得る。先勝ち(最初の出現の序数に束縛)は
        // switch 形では (__ordinals[__index] < 0) ガード、直比較形では (__ordN < 0) ガードが担う。
        // 両経路を同名列 2 回の入力で実行し、先の値が採られることを確認する。
        // Duplicate column names occur in UNION / JOIN SELECT *. First-match-wins is enforced by the
        // (__ordinals[__index] < 0) guard on the switch path and the (__ordN < 0) guard on the direct path;
        // both are driven here with a doubled column and must bind the first occurrence.
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(long), "Id")
            ],
            Rows([100L, "A", 200L]))));

        var accessor = new MappingAccessor();
        var wide = accessor.QueryWideEntities(con);

        Assert.Single(wide);
        Assert.Equal(100L, wide[0].Id);
        Assert.Equal("A", wide[0].Name);

        using var con2 = new MockDbConnection();
        con2.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(string), "Name"),
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name")
            ],
            Rows(["first", 1L, "second"]))));

        var narrow = accessor.QueryEntities(con2);

        Assert.Single(narrow);
        Assert.Equal(1L, narrow[0].Id);
        Assert.Equal("first", narrow[0].Name);
    }

    [Fact]
    public void NonAsciiColumnNamesResolveViaDirectComparison()
    {
        // 閾値超えでもサンプリング位置を ASCII に取れないため直比較へ落ちる経路。大小無視の対象が無い代わりに、
        // 順不同・部分列で正しく解けることを確認する。
        using var con = new MockDbConnection();
        con.SetupCommand(static cmd => cmd.SetupResult(new MockDataReader(
            [
                new MockColumn(typeof(string), "更新者"),
                new MockColumn(typeof(string), "氏名"),
                new MockColumn(typeof(int), "社員番号"),
                new MockColumn(typeof(string), "所属名称")
            ],
            Rows(["updater", "山田太郎", 42, "開発部"]))));

        var accessor = new MappingAccessor();
        var list = accessor.QueryNonAsciiEntities(con);

        Assert.Single(list);
        Assert.Equal(42, list[0].EmployeeNo);
        Assert.Equal("山田太郎", list[0].FullName);
        Assert.Equal("開発部", list[0].DepartmentName);
        Assert.Equal("updater", list[0].UpdatedBy);
        Assert.Equal("unset", list[0].PostalCode);
    }
}
