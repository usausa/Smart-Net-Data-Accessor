namespace Smart.Data.Accessor.Generator.Tests;

// Verifies that the source generators report each wired diagnostic for the offending input,
// and that the newly wired SDA0101 does not false-positive on ordinary helper methods.
public partial class DiagnosticTests
{
    // ---- Core generator (SDA) ---------------------------------------------------------------

    [Fact]
    public void Sda0001InvalidClassWhenNotPartialEmitsDiagnostic()
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
    public void Sda0101InvalidMethodWhenDataMethodNotPartialEmitsDiagnostic()
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
    public void Sda0101NoInvalidMethodForPlainHelperEmitsNoDiagnostic()
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
    public void Sda0401SqlNotFoundWhenNoSqlAndNoBuilderEmitsDiagnostic()
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
    public void Sda0502SqlEmptyWhenSqlFileBlankEmitsDiagnostic()
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
    public void Sda0503SqlCommentNotClosedWhenBlockCommentUnterminatedEmitsDiagnostic()
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
    public void Sda0504SqlQuoteNotClosedWhenStringLiteralUnterminatedEmitsDiagnostic()
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
    public void Sda0002DataAccessorClassNestedEmitsDiagnostic()
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
    public void Sda0003DataAccessorClassGenericEmitsDiagnostic()
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
    public void Sda0102PartialMethodAlreadyImplementedEmitsDiagnostic()
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
    public void Sda0106MethodNameDuplicatedWithinClassEmitsDiagnostic()
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
    public void Sda0004InjectNameDuplicatedEmitsDiagnostic()
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
    public void Sda0302ExecuteReturnInvalidEmitsDiagnostic()
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
    public void Sda0312QueryElementHasNoMappableColumnsEmitsDiagnostic()
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
    public void Sda0301QueryScalarPrimitiveReportsUnsupportedReturnInBothSyncAndAsyncEmitsNoDiagnostic()
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
    public void Sda0303ExecuteReaderInvalidReturnEmitsDiagnostic()
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
    public void Sda0405BuilderAndSqlBothPresentEmitsDiagnostic()
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
    public void Sda0103ExecutionKindDuplicatedEmitsDiagnostic()
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
    public void Sda0104ProcedureDirectSqlConflictEmitsDiagnostic()
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
    public void Sda0107SqlAndCommandSourceConflictEmitsDiagnostic()
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
    public void Sda0108ExecutionKindMissingForCommandSourceAttributesEmitsDiagnostic()
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
    public void Sda0211SqlTextEmptyEmitsDiagnostic()
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
    public void Sda0406SqlHasSqlFileEmitsDiagnostic()
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
    public void Sda0406SqlHasSqlFileWithMethodNameAliasEmitsDiagnostic()
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
    public void Sda0503InlineSqlParseErrorPointsInsideRawStringLiteralEmitsDiagnostic()
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
    public void Sda0504InlineSqlParseErrorPointsInsideRegularLiteralEmitsDiagnostic()
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
    public void Sda0109ReaderBehaviorInvalidMethodEmitsDiagnostic()
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

    // ------------------------------------------------------------
    // Record mapping
    // ------------------------------------------------------------

    [Fact]
    public void Sda0306RecordPrimaryConstructorPathEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed record Rec(long Id);

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Rec> Query();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Id from T"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0306");
    }

    // ------------------------------------------------------------
    // SQL file
    // ------------------------------------------------------------

    [Fact]
    public void Sda0402SqlFileNameCollisionEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = /*@ id */1"), ("Accessor.Execute", "update T set B = /*@ id */1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0402");
    }

    [Fact]
    public void Sda0403DirectSqlHasSqlFileEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [DirectSql]
                [Execute]
                public partial int Execute(string sql);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0403");
    }

    [Fact]
    public void Sda0404ProcedureHasSqlFileEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("P")]
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0404");
    }

    // ------------------------------------------------------------
    // SQL parse
    // ------------------------------------------------------------

    [Fact]
    public void Sda0505SqlUnknownPragmaEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = /*!unknown */ /*@ id */1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0505");
    }

    [Fact]
    public void Sda0508UndefinedSqlParameterEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = /*@ id */1 where B = /*@ missing */2"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0508");
    }

    [Fact]
    public void Sda0509UnusedMethodParameterEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id, int unused);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = /*@ id */1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0509");
    }

    [Fact]
    public void Sda0510SqlPropertyNotFoundEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public long Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(Entity entity);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = /*@ entity.Missing */1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0510");
    }

    // ------------------------------------------------------------
    // Method kind
    // ------------------------------------------------------------

    [Fact]
    public void Sda0208DirectionOnUnsupportedMethodEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            using System.Data;

            internal sealed class Entity
            {
                public long Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query([Direction(ParameterDirection.Output)] out int total);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Query", "select Id from T"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0208");
    }

    [Fact]
    public void Sda0210DirectSqlCommandTextDirectionEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            using System.Data;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [DirectSql]
                [Execute]
                public partial int Execute([Direction(ParameterDirection.Output)] string sql);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0210");
    }

    [Fact]
    public void Sda0304ExecuteReaderRequiresUsingEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            using System.Data.Common;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [ExecuteReader]
                public partial DbDataReader Read(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Read", "select Id from T where Id = /*@ id */1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0304");
    }

    [Fact]
    public void Sda0305AsyncEnumerableMissingEnumeratorCancellationEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public long Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IAsyncEnumerable<Entity> QueryAsync();
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.QueryAsync", "select Id from T"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0305");
    }

    // ------------------------------------------------------------
    // Inject / Provider / Profile
    // ------------------------------------------------------------

    [Fact]
    public void Sda0005InjectNameConflictsWithMemberEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            [Inject(typeof(object), "dbProvider")]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0005");
    }

    [Fact]
    public void Sda0006InjectTypeNotResolvableEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            [Inject(typeof(int), "value")]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0006");
    }

    [Fact]
    public void Sda0008ProviderNameEmptyEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            [Provider("")]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0008");
    }

    [Fact]
    public void Sda0009ProviderOnPatternAOnlyAccessorEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using Smart.Data.Accessor.Attributes;

            using System.Data.Common;

            [DataAccessor]
            [Provider("main")]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(DbConnection con, int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0009");
    }

    [Fact]
    public void Sda0010ExecuteConfigProfileInvalidEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal static class NotProfile
            {
            }

            [DataAccessor]
            [ExecuteConfig(typeof(NotProfile))]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0010");
    }

    [Fact]
    public void Sda0011ProfileCircularReferenceEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [AccessorProfile]
            [ExecuteConfig(typeof(Profile))]
            internal static class Profile
            {
            }

            [DataAccessor]
            [ExecuteConfig(typeof(Profile))]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0011");
    }

    // ------------------------------------------------------------
    // Parameter attributes
    // ------------------------------------------------------------

    [Fact]
    public void Sda0201NameDuplicatedEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Execute([Name("p")] int a, [Name("p")] int b);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = /*@ p */1"));

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0201");
    }

    [Fact]
    public void Sda0202DirectSqlFirstParamNotStringEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [DirectSql]
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0202");
    }

    [Fact]
    public void Sda0203ProcedureNameEmptyEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("")]
                [Execute]
                public partial int Execute(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0203");
    }

    [Fact]
    public void Sda0204AsyncProcedureRefParamEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            using System.Threading.Tasks;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("P")]
                [Execute]
                public partial Task<int> ExecuteAsync(int id, out int result);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0204");
    }

    [Fact]
    public void Sda0205DbTypeAttributeConflictEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            using System.Data;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("P")]
                [Execute]
                public partial int Execute([DbType(DbType.Int32)][DbType<DbType>(DbType.Int64)] int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0205");
    }

    [Fact]
    public void Sda0206DbTypeProviderEnumNotWhitelistedEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal enum CustomDbType
            {
                Value = 1
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("P")]
                [Execute]
                public partial int Execute([DbType<CustomDbType>(CustomDbType.Value)] int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0206");
    }

    [Fact]
    public void Sda0207DirectionRefKindMismatchEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            using System.Data;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("P")]
                [Execute]
                public partial int Execute([Direction(ParameterDirection.Output)] int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA0207");
    }

    [Fact]
    public void Sda1005NoKeyForBuilderEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Delete(typeof(Entity), Table = "T")]
                [Execute]
                public partial int Delete(int id);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA1005");
    }

    [Fact]
    public void Sda1006TypeMapTypeHandlerConflictEmitsDiagnostic()
    {
        // Arrange
        const string source = """
            using System;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class Conv : IValueConverter<long, DateTime>
            {
                public static DateTime FromDb(long value) => new(value);

                public static long ToDb(DateTime value) => value.Ticks;
            }

            internal sealed class Entity
            {
                [Key]
                public long Id { get; set; }

                [TypeHandler(typeof(Conv))]
                public DateTime CreatedAt { get; set; }
            }

            [DataAccessor]
            [TypeMap(typeof(DateTime), System.Data.DbType.Int64)]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity), Table = "T")]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "SDA1006");
    }

    [Fact]
    public void Sda1001BuilderInvalidContainerWhenNotPartialEmitsDiagnostic()
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
    public void Sda1003BuilderMissingTableEmitsDiagnostic()
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
    public void Sda1004BuilderSelectColumnsUnresolvableEmitsDiagnostic()
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
    public void Sda1002BuilderQueryBuilderDuplicatedEmitsDiagnostic()
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

    [Fact]
    public void Sda0012NamingValueUndefinedEmitsDiagnostic()
    {
        // SDA0012: [Naming] cast to an enum value outside NamingConvention (treated as None).
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            [Naming((NamingConvention)99)]
            internal sealed partial class Accessor
            {
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, static x => x.Id == "SDA0012");
    }

    [Fact]
    public void Sda0209ReturnValueDirectionOnArgumentEmitsDiagnostic()
    {
        const string source = """
            using System.Data;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Foo")]
                [Execute]
                public partial void Foo([Direction(ParameterDirection.ReturnValue)] out int rc);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // [Direction(ReturnValue)] is retired everywhere.
        Assert.Contains(diagnostics, x => x.Id == "SDA0209");
    }
}
