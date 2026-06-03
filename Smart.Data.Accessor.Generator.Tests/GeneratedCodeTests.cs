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

    [Fact]
    public void TypeHandlerColumnReadsViaFromDb()
    {
        // Reader side: a [TypeHandler<>] column reads TDb (long → GetInt64) then converts via
        // TConverter.FromDb. The non-nullable value-type column keeps the IsDBNull guard.
        const string source = """
            using System;
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class TicksConverter : IValueConverter<long, DateTime>
            {
                public static DateTime FromDb(long v) => new(v, DateTimeKind.Utc);
                public static long ToDb(DateTime v) => v.Ticks;
            }

            internal sealed class Entity
            {
                public long Id { get; set; }

                [TypeHandler(typeof(TicksConverter))]
                public DateTime Created { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Entity> Query();
            }
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.Query", "select Id, Created from T"));
        var text = result.AllGeneratedText;

        Assert.Contains("global::TicksConverter.FromDb(__reader.GetInt64(__o.Created))", text, System.StringComparison.Ordinal);
        Assert.Contains("IsDBNull(__o.Created) ? default! : global::TicksConverter.FromDb(", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TypeHandlerParameterBindsViaConverterOverload()
    {
        // 改善2: a [TypeHandler<>] bare-marker parameter in 2-way SQL binds via the converter-sharing
        // overload (NodeEmitter); the gen-time TicksConverter.ToDb(at) value expression disappears.
        const string source = """
            using System;
            using Smart.Data.Accessor.Attributes;
            using Smart.Data.Accessor.Converters;

            internal sealed class TicksConverter : IValueConverter<long, DateTime>
            {
                public static DateTime FromDb(long v) => new(v, DateTimeKind.Utc);
                public static long ToDb(DateTime v) => v.Ticks;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Touch([TypeHandler(typeof(TicksConverter))] DateTime at, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Touch", "update T set At = /*@ at */0 where Id = /*@ id */0")).AllGeneratedText;

        Assert.Contains("AddInParameter<global::TicksConverter, long, global::System.DateTime>(cmd, \"@p0\", at)", text, System.StringComparison.Ordinal);
        Assert.DoesNotContain("global::TicksConverter.ToDb(at)", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void EnumParameterBindsViaUnderlyingCast()
    {
        // 改善2: an enum parameter binds via the canonical (object?)(underlying) cast (shared
        // GenExpr.EnumCastValue), kept gen-time to avoid a runtime Convert.ChangeType.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            internal enum Status { A, B }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Touch(Status status, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Touch", "update T set S = /*@ status */0 where Id = /*@ id */0")).AllGeneratedText;

        Assert.Contains("AddInParameter(cmd, \"@p0\", (object?)(int)status)", text, System.StringComparison.Ordinal);
    }
}
