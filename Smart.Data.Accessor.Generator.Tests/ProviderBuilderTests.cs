namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Verifies the per-provider QueryBuilder generators emit provider-correct SQL (identifier quoting + paging) and the
// provider-specific kinds (MERGE / OUTPUT / ON DUPLICATE KEY / REPLACE / INSERT IGNORE / ON CONFLICT / RETURNING).
// All provider attributes live in the flat Smart.Data.Accessor.Attributes namespace (Sql*/MySql*/Pg* prefixes).
// Pure string assertions on the generated {Method}__QueryBuilder helper — no database.
public sealed class ProviderBuilderTests
{
    private const string Entity = """
        internal sealed class Entity
        {
            public int Id { get; set; }

            public string Name { get; set; } = string.Empty;
        }
        """;

    private static string InsertAccessor(string attr) => $$"""
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

    private static string PageAccessor(string attr) => $$"""
        using System.Collections.Generic;
        using Smart.Data.Accessor.Attributes;

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
        var text = GeneratorTestHelper.Run(InsertAccessor("SqlInsert")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"INSERT INTO [Data] ([Id], [Name]) VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MySqlInsertUsesBacktickQuoting()
    {
        var text = GeneratorTestHelper.Run(InsertAccessor("MySqlInsert")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"INSERT INTO `Data` (`Id`, `Name`) VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PostgresInsertUsesDoubleQuoteQuoting()
    {
        var text = GeneratorTestHelper.Run(InsertAccessor("PgInsert")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"INSERT INTO \\\"Data\\\" (\\\"Id\\\", \\\"Name\\\") VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlServerSelectPagingUsesOffsetFetch()
    {
        var text = GeneratorTestHelper.Run(PageAccessor("SqlSelect")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"SELECT [Id], [Name] FROM [Data] ORDER BY (SELECT NULL) OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MySqlSelectPagingUsesLimitOffset()
    {
        var text = GeneratorTestHelper.Run(PageAccessor("MySqlSelect")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"SELECT `Id`, `Name` FROM `Data` LIMIT @limit OFFSET @offset\";", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PostgresSelectPagingUsesLimitOffset()
    {
        var text = GeneratorTestHelper.Run(PageAccessor("PgSelect")).AllGeneratedText;
        Assert.Contains("cmd.CommandText = \"SELECT \\\"Id\\\", \\\"Name\\\" FROM \\\"Data\\\" LIMIT @limit OFFSET @offset\";", text, StringComparison.Ordinal);
    }

    // A1: SQL Server provider-specific MERGE upsert (matches on [Key], updates non-key columns, inserts otherwise).
    [Fact]
    public void SqlServerMergeEmitsMergeUpsert()
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
                [SqlMerge(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Save(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains(
            "cmd.CommandText = \"MERGE INTO [Data] AS T USING (SELECT @Id AS [Id], @Name AS [Name]) AS S ON (T.[Id] = S.[Id]) WHEN MATCHED THEN UPDATE SET T.[Name] = S.[Name] WHEN NOT MATCHED THEN INSERT ([Id], [Name]) VALUES (S.[Id], S.[Name]);\";",
            text,
            StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Id\", entity.Id", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@Name\", entity.Name", text, StringComparison.Ordinal);
    }

    // A2: SQL Server OUTPUT clause (returns columns from the INSERTED pseudo-table; the Output property is provider-specific).
    [Fact]
    public void SqlServerInsertOutputEmitsOutputClause()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [SqlInsert(typeof(Entity), Table = "Data", Output = "Id")]
                [ExecuteScalar]
                public partial int Add(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"INSERT INTO [Data] ([Id], [Name]) OUTPUT INSERTED.[Id] VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    // A3: MySQL INSERT ... ON DUPLICATE KEY UPDATE (non-key columns updated on conflict).
    [Fact]
    public void MySqlUpsertEmitsOnDuplicateKeyUpdate()
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
                [MySqlUpsert(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Save(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"INSERT INTO `Data` (`Id`, `Name`) VALUES (@Id, @Name) ON DUPLICATE KEY UPDATE `Name` = VALUES(`Name`)\";", text, StringComparison.Ordinal);
    }

    // A3: MySQL REPLACE INTO (same shape as INSERT).
    [Fact]
    public void MySqlReplaceEmitsReplaceInto()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [MySqlReplace(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Save(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"REPLACE INTO `Data` (`Id`, `Name`) VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    // A3: MySQL INSERT IGNORE (same shape as INSERT).
    [Fact]
    public void MySqlInsertIgnoreEmitsInsertIgnore()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [MySqlInsertIgnore(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Save(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"INSERT IGNORE INTO `Data` (`Id`, `Name`) VALUES (@Id, @Name)\";", text, StringComparison.Ordinal);
    }

    // A5: PostgreSQL INSERT ... ON CONFLICT DO UPDATE (upsert; conflict target = [Key], updates the non-key columns).
    [Fact]
    public void PgUpsertEmitsOnConflictDoUpdate()
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
                [PgUpsert(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Save(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"INSERT INTO \\\"Data\\\" (\\\"Id\\\", \\\"Name\\\") VALUES (@Id, @Name) ON CONFLICT (\\\"Id\\\") DO UPDATE SET \\\"Name\\\" = EXCLUDED.\\\"Name\\\"\";", text, StringComparison.Ordinal);
    }

    // A6: PostgreSQL RETURNING clause (returns the named columns; the Returning property is provider-specific).
    [Fact]
    public void PgInsertReturningEmitsReturningClause()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }

                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [PgInsert(typeof(Entity), Table = "Data", Returning = "Id")]
                [ExecuteScalar]
                public partial int Add(Entity entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"INSERT INTO \\\"Data\\\" (\\\"Id\\\", \\\"Name\\\") VALUES (@Id, @Name) RETURNING \\\"Id\\\"\";", text, StringComparison.Ordinal);
    }
}
