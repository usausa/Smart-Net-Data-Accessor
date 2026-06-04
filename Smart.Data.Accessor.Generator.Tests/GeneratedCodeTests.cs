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
        // CodeExpressionHelper.EnumCastValue), kept gen-time to avoid a runtime Convert.ChangeType.
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

    [Fact]
    public void ExecuteReaderEmitsWrappedReader()
    {
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [ExecuteReader]
                public partial DbDataReader Read(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Read", "select * from T")).AllGeneratedText;

        Assert.Contains("cmd.ExecuteReader(", text, System.StringComparison.Ordinal);
        Assert.Contains("global::Smart.Data.Accessor.Helpers.WrappedReader", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void AsyncExecuteReaderEmitsExecuteReaderAsync()
    {
        const string source = """
            using System.Data.Common;
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [ExecuteReader]
                public partial Task<DbDataReader> ReadAsync(DbConnection con, CancellationToken cancel = default);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.ReadAsync", "select * from T")).AllGeneratedText;

        Assert.Contains("await cmd.ExecuteReaderAsync(", text, System.StringComparison.Ordinal);
        Assert.Contains("global::Smart.Data.Accessor.Helpers.WrappedReader", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void QueryListEmitsBufferedReadLoop()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from T")).AllGeneratedText;

        Assert.Contains("cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess)", text, System.StringComparison.Ordinal);
        Assert.Contains("while (__reader.Read())", text, System.StringComparison.Ordinal);
        Assert.Contains("__list.Add(", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void AsyncQueryListEmitsReadAsyncLoop()
    {
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
                public partial Task<IReadOnlyList<Row>> ListAsync(DbConnection con, CancellationToken cancel = default);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.ListAsync", "select Id from T")).AllGeneratedText;

        Assert.Contains("await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess", text, System.StringComparison.Ordinal);
        Assert.Contains("while (await __reader.ReadAsync(", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void QueryFirstEmitsSingleReadAndDefault()
    {
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [QueryFirst]
                public partial Row? First(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.First", "select Id from T")).AllGeneratedText;

        Assert.Contains("if (__reader.Read())", text, System.StringComparison.Ordinal);
        Assert.Contains("return default!;", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ExecuteScalarEmitsConvertScalar()
    {
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [ExecuteScalar]
                public partial long Count(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Count", "select count(*) from T")).AllGeneratedText;

        Assert.Contains("global::Smart.Data.Accessor.Helpers.ExecuteHelper.ConvertScalar<long>(cmd.ExecuteScalar())", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void DirectSqlEmitsCommandTextFromParameter()
    {
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [DirectSql(SuppressWarning = true)]
                public partial int Exec(DbConnection con, string sql, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = sql;", text, System.StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@id\", id", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ProcedureEmitsStoredProcedureCommandType()
    {
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp_Do")]
                [Execute]
                public partial int Do(DbConnection con, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandType = global::System.Data.CommandType.StoredProcedure;", text, System.StringComparison.Ordinal);
        Assert.Contains("cmd.CommandText = \"usp_Do\";", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void PatternBEmitsProviderCreateConnection()
    {
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List();
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from T")).AllGeneratedText;

        Assert.Contains("this.dbProvider.CreateConnection()", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderPatternBEmitsSelectorGetProvider()
    {
        const string source = """
            using System.Collections.Generic;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } }

            [DataAccessor]
            [Provider("main")]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List();
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from T")).AllGeneratedText;

        Assert.Contains("this.providerSelector.GetProvider(\"main\").CreateConnection()", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void RecordEntityMapsViaPrimaryConstructor()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed record Row(long Id, string Name);

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, Name from T")).AllGeneratedText;

        // spec §7.10.5: positional record → ctor invocation `new Row(Id: ..., Name: ...)`.
        Assert.Contains("new global::Row(", text, System.StringComparison.Ordinal);
        Assert.Contains("Id: ", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void EnumAndNullableColumnsMapWithCastAndGuard()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal enum Status { A, B }

            internal sealed class Row
            {
                public long Id { get; set; }
                public Status St { get; set; }
                public int? Age { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, St, Age from T")).AllGeneratedText;

        // spec §7.9: enum 列は underlying へキャスト。Nullable 列は IsDBNull ガード。
        Assert.Contains("(global::Status)__reader.GetInt32(", text, System.StringComparison.Ordinal);
        Assert.Contains("IsDBNull(__o.Age)", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void OutParameterInfersDbTypeFromClrType()
    {
        const string source = """
            using System.Data;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Procedure("usp")]
                [Execute]
                public partial void Run(DbConnection con, [Direction(ParameterDirection.Output)] out int total);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        // spec §5.6: OUT パラメータは CLR 型から DbType を推論（InferDbTypeExpr）。
        Assert.Contains("AddOutParameter(cmd, \"@total\", global::System.Data.DbType.Int32)", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void ProviderDbTypeEmitsProviderSpecificCast()
    {
        const string source = """
            using System.Data;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Touch([DbType<SqlDbType>(SqlDbType.NVarChar)] string name, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Touch", "update T set N = /*@ name */0 where Id = /*@ id */0")).AllGeneratedText;

        // spec §1.4 F15 / §5.3.1: provider enum whitelist → SqlParameter.SqlDbType への代入。
        Assert.Contains(".SqlDbType = ", text, System.StringComparison.Ordinal);
    }
}
