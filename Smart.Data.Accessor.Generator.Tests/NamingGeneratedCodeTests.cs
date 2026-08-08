namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// [Naming] 規約([Name] が無い場合の既定名変換)のエンドツーエンド検証。解決順は [BindPrefix] と同じ
// method → class → assembly → None。適用先は Builder のテーブル名/列名/条件パラメータ列名、Query 結果
// マッピングの照合列名、[Procedure]/[DirectSql] の POCO 展開パラメータ名。Table= / [Name] の明示は常に優先。
// End-to-end coverage for the [Naming] convention (default-name conversion when no [Name] is present). It
// resolves method → class → assembly → None like [BindPrefix], and applies to the builder table / column /
// condition-parameter column names, the Query result-mapping match names, and the POCO-expanded parameter
// names of [Procedure]/[DirectSql]. An explicit Table= / [Name] always wins.
public sealed class NamingGeneratedCodeTests
{
    [Fact]
    public void ClassScopeSnakeCaseLowerAppliesToTableAndColumns()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class UserAccount
            {
                [Key]
                public int UserId { get; set; }

                public string FirstName { get; set; } = string.Empty;
            }

            [DataAccessor]
            [Naming(NamingConvention.SnakeCaseLower)]
            internal sealed partial class Accessor
            {
                [Insert(typeof(UserAccount))]
                [Execute]
                public partial int Insert(UserAccount entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // テーブル名(エンティティ型名)と列名は変換され、バインドパラメータ名はプロパティ名のまま。
        // The table name (entity type name) and column names are converted; bind parameter names stay property-based.
        Assert.Contains("INSERT INTO \\\"user_account\\\" (\\\"user_id\\\", \\\"first_name\\\") VALUES (@UserId, @FirstName)", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@UserId\", entity.UserId", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExplicitTableAndNameWinOverNaming()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class UserAccount
            {
                [Key]
                [Name("UID")]
                public int UserId { get; set; }

                public string FirstName { get; set; } = string.Empty;
            }

            [DataAccessor]
            [Naming(NamingConvention.SnakeCaseLower)]
            internal sealed partial class Accessor
            {
                [Insert(typeof(UserAccount), Table = "Users")]
                [Execute]
                public partial int Insert(UserAccount entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("INSERT INTO \\\"Users\\\" (\\\"UID\\\", \\\"first_name\\\") VALUES (@UserId, @FirstName)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MethodScopeNamingOverridesClassScope()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class UserAccount
            {
                [Key]
                public int UserId { get; set; }

                public string FirstName { get; set; } = string.Empty;
            }

            [DataAccessor]
            [Naming(NamingConvention.SnakeCaseLower)]
            internal sealed partial class Accessor
            {
                [Insert(typeof(UserAccount))]
                [Naming(NamingConvention.None)]
                [Execute]
                public partial int Insert(UserAccount entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // method スコープの None が class スコープの変換を打ち消す。
        // The method-scope None cancels the class-scope conversion.
        Assert.Contains("INSERT INTO \\\"UserAccount\\\" (\\\"UserId\\\", \\\"FirstName\\\") VALUES (@UserId, @FirstName)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void AssemblyScopeNamingApplies()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [assembly: Naming(NamingConvention.SnakeCaseUpper)]

            internal sealed class UserAccount
            {
                [Key]
                public int UserId { get; set; }

                public string FirstName { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Insert(typeof(UserAccount))]
                [Execute]
                public partial int Insert(UserAccount entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("INSERT INTO \\\"USER_ACCOUNT\\\" (\\\"USER_ID\\\", \\\"FIRST_NAME\\\") VALUES (@UserId, @FirstName)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void LowerCaseFlattensWithoutSeparators()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class UserAccount
            {
                [Key]
                public int UserId { get; set; }

                public string FirstName { get; set; } = string.Empty;
            }

            [DataAccessor]
            [Naming(NamingConvention.LowerCase)]
            internal sealed partial class Accessor
            {
                [Insert(typeof(UserAccount))]
                [Execute]
                public partial int Insert(UserAccount entity);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("INSERT INTO \\\"useraccount\\\" (\\\"userid\\\", \\\"firstname\\\") VALUES (@UserId, @FirstName)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ConditionParameterColumnUsesNaming()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class UserAccount
            {
                [Key]
                public int UserId { get; set; }
            }

            [DataAccessor]
            [Naming(NamingConvention.SnakeCaseLower)]
            internal sealed partial class Accessor
            {
                [Delete(typeof(UserAccount))]
                [Execute]
                public partial int Delete(int userId);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // WHERE の列名はパラメータ名から変換され、バインドパラメータ名はパラメータ名のまま。
        // The WHERE column name is converted from the parameter name; the bind parameter name stays as-is.
        Assert.Contains("DELETE FROM \\\"user_account\\\" WHERE \\\"user_id\\\" = @userId", text, StringComparison.Ordinal);
    }

    [Fact]
    public void QueryResultMappingMatchesConvertedColumnNames()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class UserAccount
            {
                public int UserId { get; set; }

                public string FirstName { get; set; } = string.Empty;
            }

            [DataAccessor]
            [Naming(NamingConvention.SnakeCaseLower)]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<UserAccount> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from user_account")).AllGeneratedText;

        // 序数照合は変換後の列名で行い(OrdinalIgnoreCase)、struct フィールド・書き戻しはプロパティ名のまま。
        // Ordinal matching uses the converted column names (OrdinalIgnoreCase); the struct fields / assignments stay property-based.
        Assert.Contains("global::System.String.Equals(__name, \"user_id\", global::System.StringComparison.OrdinalIgnoreCase)", text, StringComparison.Ordinal);
        Assert.Contains("global::System.String.Equals(__name, \"first_name\", global::System.StringComparison.OrdinalIgnoreCase)", text, StringComparison.Ordinal);
        Assert.Contains("if (o.UserId >= 0) entity.UserId =", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ProcedurePocoParameterNamesUseNaming()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal sealed class FooArgs
            {
                public int CategoryId { get; set; }
            }

            [DataAccessor]
            [Naming(NamingConvention.SnakeCaseLower)]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Foo")]
                [Execute]
                public partial void Foo(FooArgs args);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("AddInParameter(cmd, \"@category_id\", args.CategoryId", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NamingValueUndefined()
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
}
