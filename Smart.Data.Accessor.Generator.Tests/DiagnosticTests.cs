namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Verifies that the source generators report each wired diagnostic for the offending input,
// and that the newly wired SDA0101 does not false-positive on ordinary helper methods.
public sealed class DiagnosticTests
{
    // ---- Core generator (SDA) ---------------------------------------------------------------

    [Fact]
    public void InvalidClassWhenNotPartial()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed class Accessor
            {
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0001");
    }

    [Fact]
    public void InvalidMethodWhenDataMethodNotPartial()
    {
        // SDA0101: a method carrying a data-method attribute must be declared `partial`.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public int Delete(int id) => 0;
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0101");
    }

    [Fact]
    public void NoInvalidMethodForPlainHelper()
    {
        // Regression guard for the SDA0101 wiring: a plain helper method (no data-method
        // attribute) next to a valid generated method must NOT trigger SDA0101.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();

                public int Helper() => 42;
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data"));

        Assert.DoesNotContain(diagnostics, x => x.Id == "SDA0101");
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void SqlNotFoundWhenNoSqlAndNoBuilder()
    {
        // 要素型はマッピング可能にしておく(int だと SDA0312 が先に発火して SQL 解決に到達しない)。
        // Keep the element type mappable (an int element would fire SDA0312 before SQL resolution is reached).
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> Query();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0401");
    }

    [Fact]
    public void SqlEmptyWhenSqlFileBlank()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "   "));

        Assert.Contains(diagnostics, x => x.Id == "SDA0502");
    }

    [Fact]
    public void SqlCommentNotClosedWhenBlockCommentUnterminated()
    {
        // SqlTokenizer throws SqlTokenizerException(CommentNotClosed); BuildSqlEmitCode catches it → SDA0503.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data /* oops"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0503");
    }

    [Fact]
    public void SqlQuoteNotClosedWhenStringLiteralUnterminated()
    {
        // SqlTokenizer throws SqlTokenizerException(QuoteNotClosed); BuildSqlEmitCode catches it → SDA0504.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data where Name = 'oops"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0504");
    }

    [Fact]
    public void DataAccessorClassNested()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed partial class Outer
            {
                [DataAccessor]
                internal sealed partial class Inner
                {
                }
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0002");
    }

    [Fact]
    public void DataAccessorClassGeneric()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor<T>
            {
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0003");
    }

    [Fact]
    public void PartialMethodAlreadyImplemented()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();

                public partial int Delete() => 0;
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0102");
    }

    [Fact]
    public void MethodNameDuplicatedWithinClass()
    {
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                [MethodName("Same")]
                public partial IReadOnlyList<int> QueryA();

                [Query]
                [MethodName("Same")]
                public partial IReadOnlyList<int> QueryB();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Same", "select Value from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0106");
    }

    [Fact]
    public void InjectNameDuplicated()
    {
        // Class-level [Inject] is only processed once the accessor has at least one data method,
        // so include a valid [Execute] method (backed by a SQL file).
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal interface IServiceA
            {
            }

            internal interface IServiceB
            {
            }

            [DataAccessor]
            [Inject(typeof(IServiceA), "service")]
            [Inject(typeof(IServiceB), "service")]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0004");
    }

    [Fact]
    public void ExecuteReturnInvalid()
    {
        // [Execute] must return int/void/Task/Task<int>/ValueTask/ValueTask<int>; string is invalid.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial string Delete();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Delete", "delete from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0302");
    }

    [Fact]
    public void QueryElementHasNoMappableColumns()
    {
        // [Query] の要素型にマッピング可能な列(public settable/init プロパティ・record 主 ctor 引数)が無い場合は
        // SDA0312 で弾く(旧来は生成コードが未定義参照 CS0103 で壊れていた)。
        // A [Query] element type with no mappable columns (public settable/init property or record primary-ctor
        // parameter) is rejected with SDA0312 (previously the generated code broke with undefined references, CS0103).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<string> List(DbConnection con);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.List", "select Name from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0312");
    }

    [Fact]
    public void QueryScalarPrimitiveReportsUnsupportedReturnInBothSyncAndAsync()
    {
        // スカラー形(単一プリミティブ)の誤 Query は sync/async とも SDA0301 に揃える([ExecuteScalar] を使うべきケース)。
        // SDA0312 はコレクション要素が非マップ型の場合に限る。
        // A scalar-shaped misuse of Query (single primitive) reports SDA0301 in both sync and async (the user should
        // use [ExecuteScalar]); SDA0312 is reserved for an unmappable collection element.
        const string source = """
            using System.Threading.Tasks;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial string GetSync(DbConnection con);

                [Query]
                [MethodName("GetAsync")]
                public partial Task<string> GetAsync(DbConnection con);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(
            source,
            ("Accessor.GetSync", "select Name from Data"),
            ("Accessor.GetAsync", "select Name from Data"));

        Assert.Equal(2, diagnostics.Count(x => x.Id == "SDA0301"));
        Assert.DoesNotContain(diagnostics, x => x.Id == "SDA0312");
    }

    [Fact]
    public void ExecuteReaderInvalidReturn()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [ExecuteReader]
                public partial int Read();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Read", "select * from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0303");
    }

    [Fact]
    public void BuilderAndSqlBothPresent()
    {
        // SDA0405: a QueryBuilder attribute and a SQL file for the same method are ambiguous.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity))]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Insert", "insert into Data default values"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0405");
    }

    [Fact]
    public void ExecutionKindDuplicated()
    {
        // SDA0103: [Execute] and [Query] (both A-group) on the same method are mutually exclusive.
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                [Query]
                public partial IReadOnlyList<Entity> Go();
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Go", "select Id from T"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0103");
    }

    [Fact]
    public void ProcedureDirectSqlConflict()
    {
        // SDA0104: [Procedure] and [DirectSql] (both B-group command sources) are mutually exclusive.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("sp_Foo")]
                [DirectSql]
                [Execute]
                public partial int Go(string sql);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0104");
    }

    [Fact]
    public void SqlAndCommandSourceConflict()
    {
        // SDA0107: [Sql] は他のコマンドソース([DirectSql] / [Procedure] / QueryBuilder 属性)と併用できない。
        // SDA0107: [Sql] cannot be combined with another command source ([DirectSql] / [Procedure] / QueryBuilder).
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Sql("select 1")]
                [DirectSql]
                [Execute]
                public partial int WithDirect(DbConnection con, string sql);

                [Sql("select 1")]
                [Procedure("usp")]
                [Execute]
                public partial int WithProcedure(DbConnection con);

                [Sql("select 1")]
                [Insert(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int WithBuilder(DbConnection con, Entity entity);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Equal(3, diagnostics.Count(x => x.Id == "SDA0107"));
    }

    [Fact]
    public void ExecutionKindMissingForCommandSourceAttributes()
    {
        // SDA0108: 実行種別属性(A 群)は生成マーカーであり必須。B 群(ソース)属性は実行種別を既定しない
        // (旧仕様の [Procedure]/[DirectSql] → Execute 既定は撤回)。
        // SDA0108: the execution-kind attribute (A-group) is the generation marker and mandatory; source
        // attributes never default it (the former [Procedure]/[DirectSql] → Execute default is withdrawn).
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp")]
                public partial int ProcOnly(DbConnection con);

                [DirectSql]
                public partial int DirectOnly(DbConnection con, string sql);

                [Sql("select 1")]
                public partial int InlineOnly(DbConnection con);

                [Insert(typeof(Entity), Table = "Data")]
                public partial int BuilderOnly(DbConnection con, Entity entity);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Equal(4, diagnostics.Count(x => x.Id == "SDA0108"));
    }

    [Fact]
    public void SqlTextEmpty()
    {
        // SDA0211: [Sql("")] テキストが空 → 警告。
        // SDA0211: [Sql("")] empty SQL text -> warning.
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                [Sql("")]
                public partial int Touch(DbConnection con);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0211");
    }

    [Fact]
    public void SqlHasSqlFile()
    {
        // SDA0406: [Sql] は対応する .sql ファイルを持ってはならない(ファイルが黙って無視され食い違う罠を防ぐ)。
        // SDA0406: [Sql] must not have a corresponding .sql file (prevents the file silently diverging unused).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                [Sql("select Id from Data")]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.List", "select Id from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0406");
    }

    [Fact]
    public void SqlHasSqlFileWithMethodNameAlias()
    {
        // SDA0406 のファイル併存チェックは [MethodName] のエイリアスを含むキー({Class}.{Alias}.sql)で判定する。
        // The SDA0406 file-coexistence check keys on the [MethodName] alias ({Class}.{Alias}.sql).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                [MethodName("ListAlias")]
                [Sql("select Id from Data")]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.ListAlias", "select Id from Data"));

        Assert.Contains(diagnostics, x => x.Id == "SDA0406");
    }

    [Fact]
    public void InlineSqlParseErrorPointsInsideRawStringLiteral()
    {
        // SDA0503(コメント未閉塞)の位置が [Sql] の raw string literal 内の該当 "/*" を指す
        // (ファイル方式はメソッド宣言、従来のインラインは属性引数全体しか指せなかった)。
        // SDA0503 (unclosed comment) points at the exact "/*" inside the [Sql] raw string literal
        // (the file form points at the method declaration; inline used to point at the whole argument).
        const string source = """"
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                [Sql("""
                    update Data set Touched = 1
                    where Id = 1 /* broken
                    """)]
                public partial int Touch(DbConnection con);
            }
            """";

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // "/* broken" の "/" は 9 行目(0 基点)・raw literal のインデント 8 + "where Id = 1 "(13 文字) = 21 桁目。
        // The "/" of "/* broken" sits at line 9 (0-based), column 8 (raw indent) + 13 ("where Id = 1 ") = 21.
        var diagnostic = diagnostics.Single(x => x.Id == "SDA0503");
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;
        Assert.Equal(9, position.Line);
        Assert.Equal(21, position.Character);
    }

    [Fact]
    public void InlineSqlParseErrorPointsInsideRegularLiteral()
    {
        // 通常リテラル(エスケープ復号)でも SDA0504(クォート未閉塞)の位置が該当 "'" を指す。
        // A regular literal (escape decoding) also points SDA0504 (unclosed quote) at the exact "'".
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                [Sql("update Data set Name = 'broken")]
                public partial int Touch(DbConnection con);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // "'broken" の "'" は 7 行目(0 基点)・リテラル内容開始 10 + "update Data set Name = "(23 文字) = 33 桁目。
        // The "'" of "'broken" sits at line 7 (0-based), column 10 (content start) + 23 ("update Data set Name = ") = 33.
        var diagnostic = diagnostics.Single(x => x.Id == "SDA0504");
        var position = diagnostic.Location.GetLineSpan().StartLinePosition;
        Assert.Equal(7, position.Line);
        Assert.Equal(33, position.Character);
    }

    [Fact]
    public void ReaderBehaviorInvalidMethod()
    {
        // SDA0109: [ReaderBehavior] は [ExecuteReader] 専用(Query 形の behavior は F17 で固定)。
        // SDA0109: [ReaderBehavior] is only valid on [ExecuteReader] (Query-shape behaviors are fixed by F17).
        const string source = """
            using System.Collections.Generic;
            using System.Data;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                [Sql("select Id from Data")]
                [ReaderBehavior(CommandBehavior.SequentialAccess)]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0109");
    }

    // ---- Builders generator (SDB) -----------------------------------------------------------

    [Fact]
    public void BuilderInvalidContainerWhenNotPartial()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed class NotPartial
            {
                [Insert(typeof(Entity))]
                public int Insert(Entity entity) => 0;
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA1001");
    }

    [Fact]
    public void BuilderMissingTable()
    {
        // SDA1003: [Insert] with neither an entity type nor a Table name.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert]
                public int Insert(int id) => 0;
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA1003");
    }

    [Fact]
    public void BuilderSelectColumnsUnresolvable()
    {
        // SDA1004: [Select] with only a Table name cannot determine the column list.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Select(Table = "Data")]
                public int Query() => 0;
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA1004");
    }

    [Fact]
    public void BuilderQueryBuilderDuplicated()
    {
        // SDA1002: more than one QueryBuilder attribute on a single method.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                [Key]
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity))]
                [Update(typeof(Entity))]
                public int Modify(Entity entity) => 0;
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA1002");
    }
}
