namespace Smart.Data.Accessor.Generator.Tests;

using System.Globalization;

// .sql ファイル名の解決規則を検証する。完全一致 {Class}.{Method}.sql が最優先で、それが無い場合は
// メソッド名末尾の Async を省いたファイル名にフォールバックする。
// Verifies the .sql file-name resolution rules: an exact {Class}.{Method}.sql wins, and when no such file
// exists resolution falls back to the name with the method's trailing "Async" omitted.
public sealed class SqlFileResolutionTests
{
    private const string AsyncQueryAccessor = """
        using System.Collections.Generic;
        using System.Data.Common;
        using System.Threading;
        using System.Threading.Tasks;
        using Smart.Data.Accessor.Attributes;

        internal sealed class Row { public long Id { get; set; } }

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [Query]
            public partial Task<IReadOnlyList<Row>> ListAsync(DbConnection con, CancellationToken cancel = default);
        }
        """;

    // ------------------------------------------------------------
    // Resolution
    // ------------------------------------------------------------

    [Fact]
    public void AsyncMethodResolvesSqlFileWithAsyncOmitted()
    {
        // ListAsync() は Accessor.ListAsync.sql が無ければ Accessor.List.sql を使う。
        // ListAsync() falls back to Accessor.List.sql when Accessor.ListAsync.sql does not exist.
        var text = GeneratorTestHelper.Run(AsyncQueryAccessor, ("Accessor.List", "select Id from Trimmed")).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"select Id from Trimmed\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExactSqlFileNameWinsOverAsyncOmittedName()
    {
        // 両方あれば完全一致が勝つ(Async 省略名はフォールバックに過ぎない)。
        // With both present the exact name wins; the Async-omitted name is only a fallback.
        var text = GeneratorTestHelper.Run(
            AsyncQueryAccessor,
            ("Accessor.List", "select Id from Trimmed"),
            ("Accessor.ListAsync", "select Id from Exact")).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"select Id from Exact\";", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Trimmed", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SyncAndAsyncMethodPairShareOneSqlFile()
    {
        // 同期/非同期のペアは Accessor.List.sql 1 つを共有できる(非同期側がフォールバックで解決する)。
        // A sync/async pair can share a single Accessor.List.sql (the async half resolves through the fallback).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);

                [Query]
                public partial Task<IReadOnlyList<Row>> ListAsync(DbConnection con, CancellationToken cancel = default);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from Shared")).AllGeneratedText;

        Assert.Equal(2, CountOccurrences(text, "cmd.CommandText = \"select Id from Shared\";"));
    }

    [Fact]
    public void MethodNameAliasEndingWithAsyncResolvesSqlFileWithAsyncOmitted()
    {
        // 解決キーは [MethodName] のエイリアスなので、Async 省略もエイリアスに対して効く。
        // The resolution key is the [MethodName] alias, so the Async omission applies to the alias too.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                [MethodName("SelectAsync")]
                public partial Task<IReadOnlyList<Row>> ListAsync(DbConnection con, CancellationToken cancel = default);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Select", "select Id from Aliased")).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"select Id from Aliased\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MethodNamedAsyncKeepsItsNameInTheSqlFileName()
    {
        // メソッド名が "Async" そのものの場合は省略しない(空名のキーへ落ちない)。
        // A method named exactly "Async" is not trimmed (resolution must not fall to an empty name).
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Async(int id);
            }
            """;

        // "Accessor..sql" is what an empty trimmed name would resolve to.
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.", "update T set A = /*@ id */1"));

        Assert.Contains(diagnostics, static x => x.Id == "SDA0401");
    }

    // ------------------------------------------------------------
    // Diagnostics
    // ------------------------------------------------------------

    [Fact]
    public void Sda0401NamesTheDeclaredMethodWhenNeitherSqlFileExists()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(AsyncQueryAccessor);

        var diagnostic = diagnostics.First(static x => x.Id == "SDA0401");
        Assert.Contains("Accessor.ListAsync.sql", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
    }

    [Fact]
    public void Sda0403DirectSqlReportsSqlFileFoundWithAsyncOmitted()
    {
        // Async 省略名で見つかったファイルも「黙って無視されるファイル」なので併存エラーになる。
        // A file found through the Async-omitted name would also be silently ignored, so it is a coexistence error.
        const string source = """
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                [DirectSql]
                public partial Task<int> ExecuteAsync(string sql);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Execute", "update T set A = 1"));

        Assert.Contains(diagnostics, static x => x.Id == "SDA0403");
    }

    [Fact]
    public void Sda0406NotReportedWhenTheAsyncOmittedFileBelongsToAnotherMethod()
    {
        // Accessor.List.sql は List() のもの。ListAsync() の [Sql] とは無関係なので SDA0406 は出さない。
        // Accessor.List.sql belongs to List(); it is unrelated to the [Sql] on ListAsync(), so no SDA0406.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);

                [Query]
                [Sql("select Id from Inline")]
                public partial Task<IReadOnlyList<Row>> ListAsync(DbConnection con, CancellationToken cancel = default);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.List", "select Id from Owned"));

        Assert.DoesNotContain(diagnostics, static x => x.Id == "SDA0406");
    }

    [Fact]
    public void Sda0405NotReportedWhenTheAsyncOmittedFileBelongsToAnotherMethod()
    {
        // Builder 側も同じ。Accessor.Insert.sql は Insert() のものなので InsertAsync() には併存判定しない。
        // Same for a Builder: Accessor.Insert.sql belongs to Insert(), so InsertAsync() reports no coexistence.
        const string source = """
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Insert(Entity entity);

                [Insert(typeof(Entity))]
                [Execute]
                public partial Task<int> InsertAsync(Entity entity, CancellationToken cancel = default);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source, ("Accessor.Insert", "insert into Data default values"));

        Assert.DoesNotContain(diagnostics, static x => x.Id == "SDA0405");
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
