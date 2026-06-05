namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Per-kind SQL shape coverage for the default (ANSI) QueryBuilder provider. After the Builder restructure each
// provider owns its own (private) per-kind emit, so the canonical shapes are asserted end-to-end through the ANSI
// generator (double-quote quoting, LIMIT/OFFSET paging) rather than against a shared pure-emit function. Provider
// divergence (bracket / backtick quoting, OFFSET-FETCH paging) is covered by ProviderBuilderTests.
public sealed class AnsiQueryBuilderTests
{
    [Fact]
    public void InsertEmitsPartialClassQueryBuilderAndStatement()
    {
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
                [Insert(typeof(Entity), Table = "Users")]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("partial class Accessor", text, StringComparison.Ordinal);
        Assert.Contains("private static void Insert__QueryBuilder(", text, StringComparison.Ordinal);
        // The standard builder double-quotes identifiers; StringLiteral escapes the quotes in the C# literal.
        Assert.Contains("INSERT INTO \\\"Users\\\" (\\\"Id\\\", \\\"Name\\\") VALUES (@Id, @Name)", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Id\", entity.Id", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Name\", entity.Name", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CountEmitsCountStatement()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Count(typeof(Entity), Table = "Users")]
                [ExecuteScalar]
                public partial long Count();
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("private static void Count__QueryBuilder(", text, StringComparison.Ordinal);
        Assert.Contains("SELECT COUNT(*) FROM \\\"Users\\\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateEmitsSetAndWhereByKey()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                [Key]
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;

                public int Age { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Update(typeof(Entity), Table = "Users")]
                [Execute]
                public partial int Update(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("private static void Update__QueryBuilder(", text, StringComparison.Ordinal);
        Assert.Contains("UPDATE \\\"Users\\\" SET \\\"Name\\\" = @Name, \\\"Age\\\" = @Age WHERE \\\"Id\\\" = @k_Id", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Name\", entity.Name", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@k_Id\", entity.Id", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdateWithoutEntityEmitsSetStub()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Update(Table = "Users")]
                [Execute]
                public partial int Touch();
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("UPDATE \\\"Users\\\" SET ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectSingleEmitsSelectWhereByKey()
    {
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
                [SelectSingle(typeof(Entity), Table = "Users")]
                [QueryFirst]
                public partial Entity Get(long id);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("private static void Get__QueryBuilder(", text, StringComparison.Ordinal);
        Assert.Contains("SELECT \\\"Id\\\", \\\"Name\\\" FROM \\\"Users\\\" WHERE \\\"Id\\\" = @id", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@id\", id", text, StringComparison.Ordinal);
    }

    [Fact]
    public void TruncateEmitsTruncateTable()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Truncate(typeof(Entity), Table = "Users")]
                [Execute]
                public partial int Clear();
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("private static void Clear__QueryBuilder(", text, StringComparison.Ordinal);
        Assert.Contains("TRUNCATE TABLE \\\"Users\\\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SelectWithLimitOffsetEmitsPaging()
    {
        const string source = """
            using System.Collections.Generic;
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
                [Select(typeof(Entity), Table = "Users")]
                [Query]
                public partial IReadOnlyList<Entity> List([Limit] int limit, [Offset] int offset);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("SELECT \\\"Id\\\", \\\"Name\\\" FROM \\\"Users\\\" LIMIT @limit OFFSET @offset", text, StringComparison.Ordinal);
        // offset → limit の順で束縛される（EmitSelect）。
        // Bound offset-then-limit (EmitSelect).
        var idxOffset = text.IndexOf("AddInParameter(cmd, \"@offset\"", StringComparison.Ordinal);
        var idxLimit = text.IndexOf("AddInParameter(cmd, \"@limit\"", StringComparison.Ordinal);
        Assert.True((idxOffset >= 0) && (idxLimit > idxOffset));
    }

    [Fact]
    public void SelectWithoutEntityEmitsSelectStar()
    {
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
                [Select(Table = "Users")]
                [Query]
                public partial IReadOnlyList<Entity> All();
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("SELECT * FROM \\\"Users\\\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void EnumColumnBindsViaUnderlyingCast()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal enum Status
            {
                Active,
                Inactive,
            }

            internal sealed class Entity
            {
                [Key]
                public int Id { get; set; }

                public Status Status { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(Entity), Table = "Users")]
                [Execute]
                public partial int Insert(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // enum は underlying へキャストして束縛。
        // The enum binds via an underlying-type cast.
        Assert.Contains("(object?)(int)entity.Status", text, StringComparison.Ordinal);
    }
}
