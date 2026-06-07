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
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<int> Query();
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

        Assert.Contains(diagnostics, x => x.Id == "SDA0010");
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
                public partial int Go(string sql);
            }
            """;

        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        Assert.Contains(diagnostics, x => x.Id == "SDA0104");
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
