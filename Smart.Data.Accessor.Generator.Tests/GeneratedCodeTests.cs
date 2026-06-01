namespace Smart.Data.Accessor.Generator.Tests;

using Xunit;

// Verifies the shape of the generated code for 2-way SQL: the static fast path emits a literal
// CommandText (no StringBuilderPool), while code blocks / IN-list expansion take the dynamic
// StringBuilderPool path. Pure string assertions on the generated source — no database.
public sealed class GeneratedCodeTests
{
    private const string ExecuteAccessor = """
        using System.Collections.Generic;
        using Smart.Data.Accessor.Attributes;

        [DataAccessor]
        internal sealed partial class Accessor
        {
            [Execute]
            public partial int Run(int? id, IEnumerable<long> ids);
        }
        """;

    [Fact]
    public void StaticSqlEmitsLiteralCommandText()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Delete(int id);
            }
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.Delete", "delete from Data where Id = /*@ id */0"));
        var text = result.AllGeneratedText;

        // Static fast path: literal CommandText, direct parameter add, no pooled StringBuilder.
        Assert.Contains("cmd.CommandText = \"delete from Data where Id = @p0\";", text, System.StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@p0\", id", text, System.StringComparison.Ordinal);
        Assert.DoesNotContain("StringBuilderPool", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ConditionalSqlUsesStringBuilderPool()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Touch(int? id);
            }
            """;

        const string sql = """
            update Data set Touched = 1
            /*% if (id != null) { */
            where Id = /*@ id */0
            /*% } */
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.Touch", sql));
        var text = result.AllGeneratedText;

        // Dynamic path: pooled StringBuilder, the if-block flows through verbatim, CommandText
        // is assigned from the builder (never a precomputed literal).
        Assert.Contains("StringBuilderPool.Rent()", text, System.StringComparison.Ordinal);
        Assert.Contains("if (id != null) {", text, System.StringComparison.Ordinal);
        Assert.Contains("cmd.CommandText = __sb.ToString();", text, System.StringComparison.Ordinal);
        Assert.Contains("StringBuilderPool.Return(__sb)", text, System.StringComparison.Ordinal);
        Assert.DoesNotContain("cmd.CommandText = \"update", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void InClauseListExpandsParameters()
    {
        var result = GeneratorTestHelper.Run(ExecuteAccessor, ("Accessor.Run", "delete from Data where Id in /*@ ids */(0) and Active = /*@ id */0"));
        var text = result.AllGeneratedText;

        // /*@ ids */(...) → runtime IN-list expansion via AddInParameters; the single scalar
        // /*@ id */ still binds via AddInParameter. The presence of a multi-value parameter forces
        // the dynamic StringBuilderPool path.
        Assert.Contains("AddInParameters(cmd, \"@p0\", ids", text, System.StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@p1\", id", text, System.StringComparison.Ordinal);
        Assert.Contains("StringBuilderPool.Rent()", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void RawSqlInjectsExpressionVerbatim()
    {
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Run(string order);
            }
            """;

        // /*# order */col → the C# expression `order` is appended to the SQL text directly
        // (raw substitution, e.g. a dynamic ORDER BY column). This is a dynamic path.
        var result = GeneratorTestHelper.Run(source, ("Accessor.Run", "delete from Data order by /*# order */col"));
        var text = result.AllGeneratedText;

        Assert.Contains("__sb.Append((order)?.ToString() ?? string.Empty);", text, System.StringComparison.Ordinal);
        Assert.Contains("StringBuilderPool.Rent()", text, System.StringComparison.Ordinal);
    }
}
