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
        Assert.Contains("cmd.CommandText = \"delete from Data where Id = @p0\";", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@p0\", id", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StringBuilderPool", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UsingAndHelperPragmasEmitFileHeaderUsings()
    {
        // /*!using N */ → `using N;`, /*!helper T */ → `using static T;`.
        // The pragmas are aggregated as UsingDirective and emitted as file-header directives.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Touch();
            }
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.Touch", "update Data set N = /*!using System.Text */ /*!helper System.Math */ 1"));
        var text = result.AllGeneratedText;

        Assert.Contains("using System.Text;", text, StringComparison.Ordinal);
        Assert.Contains("using static System.Math;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OutputDirectionParameterInTwoWaySqlEmitsAddOutParameter()
    {
        // [Direction(Output)] is allowed on [Execute] (not only [Procedure]). An OUT
        // parameter referenced as a /*@ marker */ in 2-way SQL drives NodeEmitter's OUT binding path.
        const string source = """
            using System.Data;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial void Touch([Direction(ParameterDirection.Output)] out int total);
            }
            """;

        var result = GeneratorTestHelper.Run(source, ("Accessor.Touch", "update Data set Total = /*@ total */0"));
        var text = result.AllGeneratedText;

        Assert.Contains("AddOutParameter(cmd,", text, StringComparison.Ordinal);
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
        Assert.Contains("StringBuilderPool.Rent()", text, StringComparison.Ordinal);
        Assert.Contains("if (id != null) {", text, StringComparison.Ordinal);
        Assert.Contains("cmd.CommandText = __sb.ToString();", text, StringComparison.Ordinal);
        Assert.Contains("StringBuilderPool.Return(__sb)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("cmd.CommandText = \"update", text, StringComparison.Ordinal);
    }

    [Fact]
    public void InClauseListExpandsParameters()
    {
        var result = GeneratorTestHelper.Run(ExecuteAccessor, ("Accessor.Run", "delete from Data where Id in /*@ ids */(0) and Active = /*@ id */0"));
        var text = result.AllGeneratedText;

        // /*@ ids */(...) → runtime IN-list expansion via AddInParameters; the single scalar
        // /*@ id */ still binds via AddInParameter. The presence of a multi-value parameter forces
        // the dynamic StringBuilderPool path.
        Assert.Contains("AddInParameters(__sb, cmd, \"@p0\", ids", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@p1\", id", text, StringComparison.Ordinal);
        Assert.Contains("StringBuilderPool.Rent()", text, StringComparison.Ordinal);
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

        Assert.Contains("__sb.Append((order)?.ToString() ?? string.Empty);", text, StringComparison.Ordinal);
        Assert.Contains("StringBuilderPool.Rent()", text, StringComparison.Ordinal);
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
                public static DateTime FromDb(long value) => new(value, DateTimeKind.Utc);
                public static long ToDb(DateTime value) => value.Ticks;
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

        Assert.Contains("global::TicksConverter.FromDb(reader.GetInt64(o.Created))", text, StringComparison.Ordinal);
        Assert.Contains("IsDBNull(o.Created) ? default! : global::TicksConverter.FromDb(", text, StringComparison.Ordinal);
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
                public static DateTime FromDb(long value) => new(value, DateTimeKind.Utc);
                public static long ToDb(DateTime value) => value.Ticks;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                public partial int Touch([TypeHandler(typeof(TicksConverter))] DateTime at, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Touch", "update T set At = /*@ at */0 where Id = /*@ id */0")).AllGeneratedText;

        Assert.Contains("AddInParameter<global::TicksConverter, long, global::System.DateTime>(cmd, \"@p0\", at)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("global::TicksConverter.ToDb(at)", text, StringComparison.Ordinal);
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

        Assert.Contains("AddInParameter(cmd, \"@p0\", (object?)(int)status)", text, StringComparison.Ordinal);
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

        Assert.Contains("cmd.ExecuteReader(", text, StringComparison.Ordinal);
        Assert.Contains("global::Smart.Data.Accessor.Helpers.WrappedReader", text, StringComparison.Ordinal);
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

        Assert.Contains("await cmd.ExecuteReaderAsync(", text, StringComparison.Ordinal);
        Assert.Contains("global::Smart.Data.Accessor.Helpers.WrappedReader", text, StringComparison.Ordinal);
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

        Assert.Contains("cmd.ExecuteReader(global::System.Data.CommandBehavior.SingleResult)", text, StringComparison.Ordinal);
        Assert.Contains("while (__reader.Read())", text, StringComparison.Ordinal);
        Assert.Contains("__list.Add(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OrdinalResolutionToleratesMissingColumns()
    {
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } public string Name { get; set; } = string.Empty; }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, Name from T")).AllGeneratedText;

        // __From はリーダー列を 1 回走査し、事前構築の static FrozenDictionary(OrdinalIgnoreCase、列名→グループ id)で
        // 大小無視の照合を行う(SQL 識別子と同じ扱い、先勝ち)。欠落列は -1 のまま(GetOrdinal は使わない＝欠落列で
        // throw しない)。全グループ解決後は走査を打ち切る。
        // __From scans the reader's columns once, matching case-insensitively via a prebuilt static FrozenDictionary
        // (OrdinalIgnoreCase, column name → group id; SQL-identifier-like, first match wins); an absent column stays
        // -1 (GetOrdinal, which throws on a missing column, is not used). The scan stops once every group is resolved.
        Assert.Contains("global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(", text, StringComparison.Ordinal);
        Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase", text, StringComparison.Ordinal);
        Assert.Contains("[\"Id\"] = 0,", text, StringComparison.Ordinal);
        Assert.Contains("stackalloc int[2]", text, StringComparison.Ordinal);
        Assert.Contains("if (__Columns.TryGetValue(reader.GetName(__i), out var __index) && (__ordinals[__index] < 0))", text, StringComparison.Ordinal);
        Assert.Contains("if (__resolved == 2) break;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrdinal", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ToUpperInvariant", text, StringComparison.Ordinal);
        // 行マッパーは存在する列(序数 >= 0)だけプロパティへ設定する(無い列は初期値を保持)。
        // The row mapper assigns only columns present in the result set (ordinal >= 0); absent columns keep their defaults.
        Assert.Contains("if (o.Id >= 0) entity.Id =", text, StringComparison.Ordinal);
        Assert.Contains("if (o.Name >= 0) entity.Name =", text, StringComparison.Ordinal);
    }

    [Fact]
    public void InitOnlyAndRequiredPropertiesAssignInsideInitializer()
    {
        // init-only / required プロパティは初期化子外で代入できない(CS8852/CS9035)ため、行マッパーは
        // `new T { ... }` 内でガード付き三項により設定する(欠落列は default!)。settable プロパティは従来どおり
        // 存在列のみ文形式で設定する。
        // Init-only / required properties cannot be assigned outside an object initialiser (CS8852/CS9035), so the row
        // mapper sets them inside `new T { ... }` with a guarded conditional (absent columns receive default!).
        // Plain settable properties keep the statement form assigned only when present.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                public long Id { get; init; }
                public required string Name { get; set; }
                public int Age { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, Name, Age from T")).AllGeneratedText;

        Assert.Contains("Id = o.Id < 0 ? default! : (", text, StringComparison.Ordinal);
        Assert.Contains("Name = o.Name < 0 ? default! : (", text, StringComparison.Ordinal);
        Assert.Contains("if (o.Age >= 0) entity.Age =", text, StringComparison.Ordinal);
        Assert.DoesNotContain("entity.Id =", text, StringComparison.Ordinal);
        Assert.DoesNotContain("entity.Name =", text, StringComparison.Ordinal);
    }

    [Fact]
    public void QueryOverloadsShareOrdinalStructAndMapper()
    {
        // 同名オーバーロードでも序数 struct / 行マッパーは (要素型 × 列リスト) 単位で 1 度だけ emit され共有される
        // (旧来のメソッド名由来の命名では CS0102/CS0111 の重複定義になっていた)。
        // Same-name overloads share the ordinal struct / row mapper emitted once per (element type, column list)
        // (method-name-derived naming used to produce duplicate definitions, CS0102/CS0111).
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
                [MethodName("ListAsync")]
                public partial Task<IReadOnlyList<Row>> List(DbConnection con, CancellationToken cancel);
            }
            """;

        var text = GeneratorTestHelper.Run(
            source,
            ("Accessor.List", "select Id from T"),
            ("Accessor.ListAsync", "select Id from T")).AllGeneratedText;

        Assert.Equal(1, CountOccurrences(text, "private readonly struct __RowOrdinals"));
        Assert.Equal(1, CountOccurrences(text, "private static global::Row __MapRow("));
        Assert.Equal(2, CountOccurrences(text, "__RowOrdinals.__From(__reader)"));
    }

    [Fact]
    public void RecordIgnoredPositionalParameterReceivesDefault()
    {
        // record 主コンストラクタの [property: Ignore] 引数はマップ対象外だが ctor には必須のため、
        // 行マッパーは default! を渡す(省略すると CS7036 で生成コードが壊れる)。
        // A [property: Ignore] positional parameter is unmapped but still required by the ctor, so the row mapper
        // passes default! (omitting the argument would break the generated code with CS7036).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed record Row(long Id, [property: Ignore] string Temp);

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from T")).AllGeneratedText;

        Assert.Contains("Temp: default!", text, StringComparison.Ordinal);
        Assert.DoesNotContain("o.Temp", text, StringComparison.Ordinal);
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

        Assert.Contains("await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SingleResult", text, StringComparison.Ordinal);
        Assert.Contains("while (await __reader.ReadAsync(", text, StringComparison.Ordinal);
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

        Assert.Contains("if (__reader.Read())", text, StringComparison.Ordinal);
        Assert.Contains("return default!;", text, StringComparison.Ordinal);
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

        Assert.Contains("global::Smart.Data.Accessor.Helpers.ExecuteHelper.ConvertScalar<long>(cmd.ExecuteScalar())", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ExecuteScalarWithIntReturnEmitsConvertScalar()
    {
        // int は [Execute] では影響行数(ExecuteNonQuery)だが、[ExecuteScalar] では他のスカラー型と
        // 同じく ExecuteScalar + ConvertScalar<int> で読む(MethodType で分岐し、型名では分岐しない)。
        // With [Execute] an int return is the affected-row count (ExecuteNonQuery), but with
        // [ExecuteScalar] an int reads like any other scalar via ExecuteScalar + ConvertScalar<int>
        // (the emit branches on MethodType, not on the type name).
        const string source = """
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [ExecuteScalar]
                public partial int Count(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.Count", "select count(*) from T")).AllGeneratedText;

        Assert.Contains("global::Smart.Data.Accessor.Helpers.ExecuteHelper.ConvertScalar<int>(cmd.ExecuteScalar())", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteNonQuery", text, StringComparison.Ordinal);
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
                [DirectSql]
                public partial int Exec(DbConnection con, string sql, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = sql;", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@id\", id", text, StringComparison.Ordinal);
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

        Assert.Contains("cmd.CommandType = global::System.Data.CommandType.StoredProcedure;", text, StringComparison.Ordinal);
        Assert.Contains("cmd.CommandText = \"usp_Do\";", text, StringComparison.Ordinal);
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

        Assert.Contains("this.dbProvider.CreateConnection()", text, StringComparison.Ordinal);
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

        Assert.Contains("this.providerSelector.GetProvider(\"main\").CreateConnection()", text, StringComparison.Ordinal);
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

        // Positional record → ctor invocation `new Row(Id: ..., Name: ...)`.
        Assert.Contains("new global::Row(", text, StringComparison.Ordinal);
        Assert.Contains("Id: ", text, StringComparison.Ordinal);
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

        // enum 列は underlying へキャスト。Nullable 列は IsDBNull ガード。
        Assert.Contains("(global::Status)reader.GetInt32(", text, StringComparison.Ordinal);
        Assert.Contains("IsDBNull(o.Age)", text, StringComparison.Ordinal);
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

        // OUT パラメータは CLR 型から DbType を推論(InferDbTypeExpression)。
        Assert.Contains("AddOutParameter(cmd, \"@total\", global::System.Data.DbType.Int32)", text, StringComparison.Ordinal);
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

        // provider enum whitelist → SqlParameter.SqlDbType への代入。
        Assert.Contains(".SqlDbType = ", text, StringComparison.Ordinal);
    }
}
