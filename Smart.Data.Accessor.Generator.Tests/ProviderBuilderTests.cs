namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Phase 7: verifies the per-provider QueryBuilder generators emit provider-correct SQL (identifier
// quoting + paging), driven by the shared dialect-parameterized engine. Pure string assertions on
// the generated {Method}__QueryBuilder helper — no database.
public sealed class ProviderBuilderTests
{
    private const string Entity = """
        internal sealed class Entity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }
        """;

    private static string InsertAccessor(string ns, string attr) => $$"""
        using {{ns}};
        using Smart.Data.Accessor.Attributes;

        {{Entity}}

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [{{attr}}(typeof(Entity), Table = "Data")]
            [Execute]
            public partial int Insert(Entity entity);
        }
        """;

    private static string PageAccessor(string ns, string attr) => $$"""
        using System.Collections.Generic;
        using {{ns}};
        using Smart.Data.Accessor.Attributes;
        using Smart.Data.Accessor.Builders;

        {{Entity}}

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [{{attr}}(typeof(Entity), Table = "Data")]
            [Query]
            public partial IReadOnlyList<Entity> Page([Limit] int limit, [Offset] int offset);
        }
        """;

    [Fact]
    public void SqlServerInsertUsesBracketQuoting()
    {
        var text = GeneratorTestHelper.Run(InsertAccessor("Smart.Data.Accessor.Builders.SqlServer", "SqlServerInsert")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"INSERT INTO [Data] ([Id], [Name]) VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MySqlInsertUsesBacktickQuoting()
    {
        var text = GeneratorTestHelper.Run(InsertAccessor("Smart.Data.Accessor.Builders.MySql", "MySqlInsert")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"INSERT INTO `Data` (`Id`, `Name`) VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PostgresInsertUsesDoubleQuoteQuoting()
    {
        var text = GeneratorTestHelper.Run(InsertAccessor("Smart.Data.Accessor.Builders.Postgres", "PostgresInsert")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"INSERT INTO \\\"Data\\\" (\\\"Id\\\", \\\"Name\\\") VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlServerSelectPagingUsesOffsetFetch()
    {
        var text = GeneratorTestHelper.Run(PageAccessor("Smart.Data.Accessor.Builders.SqlServer", "SqlServerSelect")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"SELECT [Id], [Name] FROM [Data] ORDER BY (SELECT NULL) OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MySqlSelectPagingUsesLimitOffset()
    {
        var text = GeneratorTestHelper.Run(PageAccessor("Smart.Data.Accessor.Builders.MySql", "MySqlSelect")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"SELECT `Id`, `Name` FROM `Data` LIMIT @limit OFFSET @offset\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PostgresSelectPagingUsesLimitOffset()
    {
        var text = GeneratorTestHelper.Run(PageAccessor("Smart.Data.Accessor.Builders.Postgres", "PostgresSelect")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"SELECT \\\"Id\\\", \\\"Name\\\" FROM \\\"Data\\\" LIMIT @limit OFFSET @offset\";", text, StringComparison.Ordinal);
    }
}
