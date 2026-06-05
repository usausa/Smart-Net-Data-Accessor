namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// C1: per-kind end-to-end coverage of the three non-default providers (SqlServer / MySql / Postgres), confirming each
// dialect's identifier quoting is applied across every standard builder kind — not just Insert/Select (ProviderBuilderTests).
// Doubles as the B1 drift guard: the standard kinds share one SQL shape across providers, differing only by quoting.
// C3: the without-entity fallback shapes (SELECT * / UPDATE SET) per provider.
public sealed class ProviderKindBuilderTests
{
    // Escape a raw SQL string the way the generator renders it into a C# string literal, then assert the whole
    // `cmd.CommandText = "...";` line is present. SqlServer brackets / MySql backticks need no escaping; Postgres
    // double quotes become \".
    private static void AssertCommandText(string generatedText, string rawSql)
    {
        var literal = "cmd.CommandText = \"" + rawSql.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal) + "\";";
        Assert.Contains(literal, generatedText, StringComparison.Ordinal);
    }

    // === C1: standard kinds × 3 providers ===

    [Theory]
    [InlineData("Smart.Data.Accessor.Attributes.SqlServer", "Sql", "UPDATE [Data] SET [Name] = @Name, [Age] = @Age WHERE [Id] = @k_Id")]
    [InlineData("Smart.Data.Accessor.Attributes.MySql", "MySql", "UPDATE `Data` SET `Name` = @Name, `Age` = @Age WHERE `Id` = @k_Id")]
    [InlineData("Smart.Data.Accessor.Attributes.Postgres", "Pg", "UPDATE \"Data\" SET \"Name\" = @Name, \"Age\" = @Age WHERE \"Id\" = @k_Id")]
    public void ProviderUpdateQuotesAllIdentifiers(string ns, string attr, string expected)
    {
        var source = $$"""
            using {{ns}};
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
                [{{attr}}Update(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Upd(Entity entity);
            }
            """;
        AssertCommandText(GeneratorTestHelper.Run(source).AllGeneratedText, expected);
    }

    [Theory]
    [InlineData("Smart.Data.Accessor.Attributes.SqlServer", "Sql", "DELETE FROM [Data] WHERE [Id] = @id")]
    [InlineData("Smart.Data.Accessor.Attributes.MySql", "MySql", "DELETE FROM `Data` WHERE `Id` = @id")]
    [InlineData("Smart.Data.Accessor.Attributes.Postgres", "Pg", "DELETE FROM \"Data\" WHERE \"Id\" = @id")]
    public void ProviderDeleteQuotesAllIdentifiers(string ns, string attr, string expected)
    {
        var source = $$"""
            using {{ns}};
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                [Key]
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [{{attr}}Delete(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Del(int id);
            }
            """;
        AssertCommandText(GeneratorTestHelper.Run(source).AllGeneratedText, expected);
    }

    [Theory]
    [InlineData("Smart.Data.Accessor.Attributes.SqlServer", "Sql", "SELECT [Id], [Name] FROM [Data] WHERE [Id] = @id")]
    [InlineData("Smart.Data.Accessor.Attributes.MySql", "MySql", "SELECT `Id`, `Name` FROM `Data` WHERE `Id` = @id")]
    [InlineData("Smart.Data.Accessor.Attributes.Postgres", "Pg", "SELECT \"Id\", \"Name\" FROM \"Data\" WHERE \"Id\" = @id")]
    public void ProviderSelectSingleQuotesAllIdentifiers(string ns, string attr, string expected)
    {
        var source = $$"""
            using {{ns}};
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
                [{{attr}}SelectSingle(typeof(Entity), Table = "Data")]
                [QueryFirst]
                public partial Entity Get(int id);
            }
            """;
        AssertCommandText(GeneratorTestHelper.Run(source).AllGeneratedText, expected);
    }

    [Theory]
    [InlineData("Smart.Data.Accessor.Attributes.SqlServer", "Sql", "SELECT COUNT(*) FROM [Data]")]
    [InlineData("Smart.Data.Accessor.Attributes.MySql", "MySql", "SELECT COUNT(*) FROM `Data`")]
    [InlineData("Smart.Data.Accessor.Attributes.Postgres", "Pg", "SELECT COUNT(*) FROM \"Data\"")]
    public void ProviderCountQuotesTable(string ns, string attr, string expected)
    {
        var source = $$"""
            using {{ns}};
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [{{attr}}Count(typeof(Entity), Table = "Data")]
                [ExecuteScalar]
                public partial long Cnt();
            }
            """;
        AssertCommandText(GeneratorTestHelper.Run(source).AllGeneratedText, expected);
    }

    [Theory]
    [InlineData("Smart.Data.Accessor.Attributes.SqlServer", "Sql", "TRUNCATE TABLE [Data]")]
    [InlineData("Smart.Data.Accessor.Attributes.MySql", "MySql", "TRUNCATE TABLE `Data`")]
    [InlineData("Smart.Data.Accessor.Attributes.Postgres", "Pg", "TRUNCATE TABLE \"Data\"")]
    public void ProviderTruncateQuotesTable(string ns, string attr, string expected)
    {
        var source = $$"""
            using {{ns}};
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [{{attr}}Truncate(typeof(Entity), Table = "Data")]
                [Execute]
                public partial int Clr();
            }
            """;
        AssertCommandText(GeneratorTestHelper.Run(source).AllGeneratedText, expected);
    }

    // === C3: without-entity fallback shapes × 3 providers (these also raise SDA1004; the source is still generated) ===

    [Theory]
    [InlineData("Smart.Data.Accessor.Attributes.SqlServer", "Sql", "SELECT * FROM [Data]")]
    [InlineData("Smart.Data.Accessor.Attributes.MySql", "MySql", "SELECT * FROM `Data`")]
    [InlineData("Smart.Data.Accessor.Attributes.Postgres", "Pg", "SELECT * FROM \"Data\"")]
    public void ProviderSelectWithoutEntityEmitsSelectStar(string ns, string attr, string expected)
    {
        var source = $$"""
            using System.Collections.Generic;
            using {{ns}};
            using Smart.Data.Accessor.Attributes;

            internal sealed class Entity
            {
                public int Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [{{attr}}Select(Table = "Data")]
                [Query]
                public partial IReadOnlyList<Entity> All();
            }
            """;
        AssertCommandText(GeneratorTestHelper.Run(source).AllGeneratedText, expected);
    }

    [Theory]
    [InlineData("Smart.Data.Accessor.Attributes.SqlServer", "Sql", "UPDATE [Data] SET ")]
    [InlineData("Smart.Data.Accessor.Attributes.MySql", "MySql", "UPDATE `Data` SET ")]
    [InlineData("Smart.Data.Accessor.Attributes.Postgres", "Pg", "UPDATE \"Data\" SET ")]
    public void ProviderUpdateWithoutEntityEmitsSetStub(string ns, string attr, string expected)
    {
        var source = $$"""
            using {{ns}};
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [{{attr}}Update(Table = "Data")]
                [Execute]
                public partial int Touch();
            }
            """;
        AssertCommandText(GeneratorTestHelper.Run(source).AllGeneratedText, expected);
    }
}
