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
        Assert.Contains("IsDBNull(o.Created) ? default(global::System.DateTime)! : global::TicksConverter.FromDb(", text, StringComparison.Ordinal);
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
        // 9 グループ(閾値超え)＝FrozenDictionary 形。narrow(8 グループ以下)の直比較形は
        // NarrowEntityOrdinalResolutionUsesDirectComparison で検証する。
        // Nine groups (above the threshold) = the FrozenDictionary form; the narrow (<= 8 group) direct-comparison
        // form is covered by NarrowEntityOrdinalResolutionUsesDirectComparison.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                public long Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public int Age { get; set; }
                public double Score { get; set; }
                public bool Active { get; set; }
                public int Status { get; set; }
                public string Description { get; set; } = string.Empty;
                public int Category { get; set; }
                public string Tag { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, Name, Age, Score, Active, Status, Description, Category, Tag from T")).AllGeneratedText;

        // __From はリーダー列を 1 回走査し、事前構築の static FrozenDictionary(OrdinalIgnoreCase、列名→グループ id)で
        // 大小無視の照合を行う(SQL 識別子と同じ扱い、先勝ち)。欠落列は -1 のまま(GetOrdinal は使わない＝欠落列で
        // throw しない)。全グループ解決後は走査を打ち切る。
        // __From scans the reader's columns once, matching case-insensitively via a prebuilt static FrozenDictionary
        // (OrdinalIgnoreCase, column name → group id; SQL-identifier-like, first match wins); an absent column stays
        // -1 (GetOrdinal, which throws on a missing column, is not used). The scan stops once every group is resolved.
        Assert.Contains("global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(", text, StringComparison.Ordinal);
        Assert.Contains("global::System.StringComparer.OrdinalIgnoreCase", text, StringComparison.Ordinal);
        Assert.Contains("[\"Id\"] = 0,", text, StringComparison.Ordinal);
        Assert.Contains("stackalloc int[9]", text, StringComparison.Ordinal);
        Assert.Contains("if (__Columns.TryGetValue(reader.GetName(__i), out var __index) && (__ordinals[__index] < 0))", text, StringComparison.Ordinal);
        Assert.Contains("if (__resolved == 9) break;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrdinal", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ToUpperInvariant", text, StringComparison.Ordinal);
        // 行マッパーは存在する列(序数 >= 0)だけプロパティへ設定する(無い列は初期値を保持)。
        // The row mapper assigns only columns present in the result set (ordinal >= 0); absent columns keep their defaults.
        Assert.Contains("if (o.Id >= 0) entity.Id =", text, StringComparison.Ordinal);
        Assert.Contains("if (o.Name >= 0) entity.Name =", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NarrowEntityOrdinalResolutionUsesDirectComparison()
    {
        // グループ数 8 以下の narrow エンティティは FrozenDictionary を使わず String.Equals(OrdinalIgnoreCase) の
        // 直比較で解決する(2026-07 PoC 実測で 1〜8 グループ全形 2.5〜11 倍高速・static 辞書と型初期化子も不要)。
        // 意味論は同一：大小無視・先勝ち・欠落列 -1・全解決で走査打ち切り。
        // A narrow entity (<= 8 groups) resolves via direct String.Equals(OrdinalIgnoreCase) comparisons instead of
        // the FrozenDictionary (2026-07 PoC measurements: 2.5-11x faster across every shape for 1-8 groups, and the
        // static dictionary + type initializer disappear). Semantics are identical: case-insensitive, first match
        // wins, absent columns stay -1, and the scan stops on full resolution.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                public long Id { get; set; }
                public string Name { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, Name from T")).AllGeneratedText;

        Assert.Contains("var __ord0 = -1;", text, StringComparison.Ordinal);
        Assert.Contains("var __ord1 = -1;", text, StringComparison.Ordinal);
        Assert.Contains("if ((__ord0 < 0) && global::System.String.Equals(__name, \"Id\", global::System.StringComparison.OrdinalIgnoreCase))", text, StringComparison.Ordinal);
        Assert.Contains("else if ((__ord1 < 0) && global::System.String.Equals(__name, \"Name\", global::System.StringComparison.OrdinalIgnoreCase))", text, StringComparison.Ordinal);
        Assert.Contains("if ((__ord1 >= 0)) break;", text, StringComparison.Ordinal);
        Assert.Contains("return new(__ord0, __ord1);", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
        Assert.DoesNotContain("stackalloc", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrdinal", text, StringComparison.Ordinal);
    }

    [Fact]
    public void InitOnlyAndRequiredPropertiesAssignInsideInitializer()
    {
        // init-only / required プロパティは初期化子外で代入できない(CS8852/CS9035)ため、行マッパーは
        // `new T { ... }` 内でガード付き三項により設定する(欠落列は default(プロパティ型))。settable プロパティは
        // 従来どおり存在列のみ文形式で設定する。
        // Init-only / required properties cannot be assigned outside an object initialiser (CS8852/CS9035), so the row
        // mapper sets them inside `new T { ... }` with a guarded conditional (absent columns receive a property-typed
        // default). Plain settable properties keep the statement form assigned only when present.
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

        Assert.Contains("Id = o.Id < 0 ? default(long)! : (", text, StringComparison.Ordinal);
        Assert.Contains("Name = o.Name < 0 ? default(string)! : (", text, StringComparison.Ordinal);
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

    [Fact]
    public void RecordIgnoredParameterWithDefaultValueOmitsArgument()
    {
        // 宣言既定値を持つ [property: Ignore] 引数は名前付き引数ごと省略され、宣言既定値が生きる
        // (default! を渡すと宣言既定値が null 等で上書きされてしまう)。
        // An [property: Ignore] parameter with a declared default value omits the named argument entirely so the
        // declared default applies (passing default! would override it with null etc.).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed record Row(long Id, [property: Ignore] string Temp, [property: Ignore] string Source = "db");

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from T")).AllGeneratedText;

        Assert.Contains("Temp: default!", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Source:", text, StringComparison.Ordinal);
    }

    [Fact]
    public void GenericElementTypeProducesValidGeneratedNames()
    {
        // ジェネリック要素型では型引数を除いた短名で命名する('<' '>' や型引数内の '.' が識別子に混入すると
        // 生成コードがコンパイル不能になる)。閉じたジェネリック同士は連番で一意化される。
        // A generic element type derives names from the short name with type arguments stripped ('<' '>' or dots
        // inside type arguments would make the generated identifiers uncompilable). Distinct closed generics are
        // uniqued with a numeric suffix.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Wrapper<T>
            {
                public long Id { get; set; }
                public T Value { get; set; } = default!;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Wrapper<int>> ListInt(DbConnection con);

                [Query]
                public partial IReadOnlyList<Wrapper<long>> ListLong(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(
            source,
            ("Accessor.ListInt", "select Id, Value from T"),
            ("Accessor.ListLong", "select Id, Value from T")).AllGeneratedText;

        Assert.Contains("private readonly struct __WrapperOrdinals", text, StringComparison.Ordinal);
        Assert.Contains("private readonly struct __Wrapper1Ordinals", text, StringComparison.Ordinal);
        Assert.Contains("private static global::Wrapper<int> __MapWrapper(", text, StringComparison.Ordinal);
        Assert.Contains("private static global::Wrapper<long> __MapWrapper1(", text, StringComparison.Ordinal);
        Assert.DoesNotContain("<int>Ordinals", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RequiredUnmappedClassMembersReceiveDefaultInInitializer()
    {
        // マップ対象外の required メンバ([Ignore] 付き・非 public)も初期化子で default! を設定する
        // (設定しないと生成コードが CS9035 でコンパイル不能。required は包含型と同等以上の可視性が
        // 言語規則で保証されるため、同一アセンブリの生成コードから常に設定できる)。
        // Required members excluded from mapping ([Ignore] / non-public) still receive default! inside the
        // initializer (otherwise the generated code breaks with CS9035; required members are at least as visible
        // as the containing type, so same-assembly generated code can always assign them).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                public long Id { get; set; }

                [Ignore]
                public required string Secret { get; set; }

                internal required string Hidden { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from T")).AllGeneratedText;

        Assert.Contains("Secret = default!,", text, StringComparison.Ordinal);
        Assert.Contains("Hidden = default!,", text, StringComparison.Ordinal);
        Assert.Contains("if (o.Id >= 0) entity.Id", text, StringComparison.Ordinal);
        Assert.DoesNotContain("o.Secret", text, StringComparison.Ordinal);
        Assert.DoesNotContain("o.Hidden", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordNonPositionalRequiredMemberReceivesDefaultInInitializer()
    {
        // record 主 ctor 外(非位置)の required メンバはマップ対象外だが、ctor 呼び出し後の初期化子で
        // default! を設定する(設定しないと CS9035 で生成コードが壊れる)。
        // A required member outside the record primary ctor (non-positional) is unmapped, but the trailing
        // initializer sets default! (otherwise the generated code breaks with CS9035).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed record Row(long Id)
            {
                public required string Name { get; init; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id from T")).AllGeneratedText;

        Assert.Contains(") { Name = default! };", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Name: ", text, StringComparison.Ordinal);
        Assert.DoesNotContain("o.Name", text, StringComparison.Ordinal);
    }

    [Fact]
    public void OrdinalStructCtorAssignsFieldsWithThisQualifier()
    {
        // 序数 struct の ctor 代入は this. 修飾する：p{n} と同名のプロパティ(＝フィールド p{n})があっても
        // パラメータに隠蔽されず、フィールドへ正しく代入される(無修飾だとパラメータ自己代入で列 0 を誤読)。
        // Ordinal-struct ctor assignments are this.-qualified: a property named p{n} (= field p{n}) is not
        // shadowed by the parameter (unqualified assignment would self-assign the parameter and misread column 0).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                public int p1 { get; set; }
                public long Id { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select p1, Id from T")).AllGeneratedText;

        Assert.Contains("this.p1 = p0;", text, StringComparison.Ordinal);
        Assert.Contains("this.Id = p1;", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DerivedGeneratedNamesAvoidCrossCollision()
    {
        // 一意化は短名単位ではなく生成識別子全体で行う：エンティティ "MapFoo" の struct(__MapFooOrdinals)と
        // エンティティ "FooOrdinals" のマッパー(__MapFooOrdinals)が交差衝突するため、後者は連番になる。
        // Uniquing runs across ALL generated identifiers, not per short name: entity "MapFoo"'s struct
        // (__MapFooOrdinals) and entity "FooOrdinals"'s mapper (__MapFooOrdinals) cross-collide, so the latter
        // takes a numeric suffix.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class MapFoo { public long Id { get; set; } }

            internal sealed class FooOrdinals { public long Id { get; set; } }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<MapFoo> ListA(DbConnection con);

                [Query]
                public partial IReadOnlyList<FooOrdinals> ListB(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(
            source,
            ("Accessor.ListA", "select Id from T"),
            ("Accessor.ListB", "select Id from T")).AllGeneratedText;

        Assert.Contains("private readonly struct __MapFooOrdinals", text, StringComparison.Ordinal);
        Assert.Contains("private static global::MapFoo __MapMapFoo(", text, StringComparison.Ordinal);
        Assert.Contains("private readonly struct __FooOrdinals1Ordinals", text, StringComparison.Ordinal);
        Assert.Contains("private static global::FooOrdinals __MapFooOrdinals1(", text, StringComparison.Ordinal);
    }

    [Fact]
    public void StructInternalNamesAvoidPropertyNameCollision()
    {
        // struct 内部名(__Columns / __From)はフィールド名＝プロパティ名と衝突し得るため、衝突時は連番になる
        // (無対策だと CS0102 の重複定義で生成コードが壊れる)。9 グループ(閾値超え)で FrozenDictionary 形の
        // __Columns 衝突と __From 衝突を同時に検証する(直比較形の __From 衝突は下のテスト)。
        // Struct-internal names (__Columns / __From) can collide with field names (= property names); a collision
        // takes a numeric suffix (otherwise the generated code breaks with duplicate definitions, CS0102). Nine
        // groups (above the threshold) verify both the FrozenDictionary-form __Columns collision and the __From
        // collision (the direct-comparison __From collision is covered below).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                public long Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public int Age { get; set; }
                public double Score { get; set; }
                public bool Active { get; set; }
                public int Status { get; set; }
                public string Tag { get; set; } = string.Empty;
                public int __Columns { get; set; }
                public int __From { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, Name, Age, Score, Active, Status, Tag, __Columns, __From from T")).AllGeneratedText;

        Assert.Contains("FrozenDictionary<string, int> __Columns1 =", text, StringComparison.Ordinal);
        Assert.Contains("if (__Columns1.TryGetValue(reader.GetName(__i)", text, StringComparison.Ordinal);
        Assert.Contains(" __From1(global::System.Data.Common.DbDataReader reader)", text, StringComparison.Ordinal);
        Assert.Contains("__RowOrdinals.__From1(__reader)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void DirectComparisonFromNameAvoidsPropertyNameCollision()
    {
        // 直比較形(閾値以下)でも __From はフィールド名＝プロパティ名と衝突し得るため連番になる
        // (直比較形は静的辞書を持たないので __Columns 衝突は起きない)。
        // The direct-comparison form (at or below the threshold) also renames __From on a field-name collision
        // (it has no static dictionary, so a __Columns collision cannot occur).
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                public long Id { get; set; }
                public int __From { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, __From from T")).AllGeneratedText;

        Assert.Contains(" __From1(global::System.Data.Common.DbDataReader reader)", text, StringComparison.Ordinal);
        Assert.Contains("__RowOrdinals.__From1(__reader)", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
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
                [Execute]
                public partial int Exec(DbConnection con, string sql, int id);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = sql;", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@id\", id", text, StringComparison.Ordinal);
    }

    [Fact]
    public void InlineSqlStaticEmitsCommandTextLiteral()
    {
        // [Sql]: インライン 2-way SQL。静的 SQL は .sql ファイル方式と同じく CommandText リテラル直埋めになり、
        // raw string literal の改行はトークナイザが空白 1 個に正規化する。SQL ファイルは不要。
        // [Sql]: inline 2-way SQL. Static SQL embeds a CommandText literal exactly like the .sql-file form;
        // newlines in the raw string literal are normalised to single spaces by the tokenizer. No SQL file needed.
        const string source = """"
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row { public long Id { get; set; } public string Name { get; set; } = string.Empty; }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                [Sql("""
                    SELECT Id, Name FROM Data
                    ORDER BY Id
                    """)]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """";

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.CommandText = \"SELECT Id, Name FROM Data ORDER BY Id\";", text, StringComparison.Ordinal);
        Assert.DoesNotContain("StringBuilderPool", text, StringComparison.Ordinal);
    }

    [Fact]
    public void InlineSqlDynamicBindsParametersAndBranches()
    {
        // [Sql] の 2-way ディレクティブ(/*% */ 条件分岐・/*@ */ バインド)もファイル方式と同一の動的 emit になる。
        // 2-way directives in [Sql] (/*% */ branches, /*@ */ binds) take the same dynamic emit as the file form.
        const string source = """
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Execute]
                [Sql("update Data set Touched = 1 /*% if (id != null) { */ where Id = /*@ id */0 /*% } */")]
                public partial int Touch(int? id);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("StringBuilderPool.Rent()", text, StringComparison.Ordinal);
        Assert.Contains("if (id != null) {", text, StringComparison.Ordinal);
        Assert.Contains("AddInParameter(cmd, \"@p0\", id", text, StringComparison.Ordinal);
        Assert.Contains("cmd.CommandText = __sb.ToString();", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ReaderBehaviorComposesIntoExecuteReader()
    {
        // [ReaderBehavior]: Pattern A(接続引数)は接続状態の三項に OR、Pattern B(接続所有)はそのまま渡す。
        // 複数フラグは名前付き OR に分解される。
        // [ReaderBehavior]: Pattern A (connection argument) ORs onto the connection-state conditional; Pattern B
        // (owned connection) passes it as-is. Multiple flags decompose into a named OR.
        const string source = """
            using System.Data;
            using System.Data.Common;
            using System.Threading;
            using System.Threading.Tasks;
            using Smart.Data.Accessor.Attributes;

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [ExecuteReader]
                [Sql("select * from Data")]
                [ReaderBehavior(CommandBehavior.SequentialAccess)]
                public partial DbDataReader Read(DbConnection con);

                [ExecuteReader]
                [Sql("select * from Data")]
                [ReaderBehavior(CommandBehavior.SingleResult | CommandBehavior.SequentialAccess)]
                public partial Task<DbDataReader> ReadAsync(CancellationToken cancel);
            }
            """;

        var text = GeneratorTestHelper.Run(source).AllGeneratedText;

        Assert.Contains("cmd.ExecuteReader((__wasClosed ? global::System.Data.CommandBehavior.CloseConnection : global::System.Data.CommandBehavior.Default) | global::System.Data.CommandBehavior.SequentialAccess)", text, StringComparison.Ordinal);
        Assert.Contains("await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SequentialAccess, ", text, StringComparison.Ordinal);
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
                public Status? Kind { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select Id, St, Age, Kind from T")).AllGeneratedText;

        // enum 列は underlying へキャスト。Nullable 列は IsDBNull ガード付きで、default はプロパティ型で型付けされる。
        // 三項式の自然型は typed アーム側に決まるため、素の default! だと DB NULL が int? へ 0 として入ってしまう。
        Assert.Contains("(global::Status)reader.GetInt32(", text, StringComparison.Ordinal);
        Assert.Contains("IsDBNull(o.Age) ? default(int?)! : ", text, StringComparison.Ordinal);
        Assert.Contains("IsDBNull(o.Kind) ? default(global::Status?)! : ", text, StringComparison.Ordinal);
        Assert.DoesNotContain(" ? default! : ", text, StringComparison.Ordinal);
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
        // 生成コードは Microsoft.Data.SqlClient.SqlParameter へキャストする(実プロジェクトでは SqlClient 参照が
        // 前提)。ハーネスは SqlClient を参照しないため、実コンパイル検証用に同名の fake を宣言する。
        // The generated code casts to Microsoft.Data.SqlClient.SqlParameter (real projects reference SqlClient).
        // The harness does not, so declare a same-named fake for the compile verification.
        const string source = """
            using System.Data;
            using Smart.Data.Accessor.Attributes;

            namespace Microsoft.Data.SqlClient
            {
                internal sealed class SqlParameter : global::System.Data.Common.DbParameter
                {
                    public global::System.Data.SqlDbType SqlDbType { get; set; }
                    public override global::System.Data.DbType DbType { get; set; }
                    public override global::System.Data.ParameterDirection Direction { get; set; }
                    public override bool IsNullable { get; set; }
                    public override string ParameterName { get; set; } = "";
                    public override int Size { get; set; }
                    public override string SourceColumn { get; set; } = "";
                    public override bool SourceColumnNullMapping { get; set; }
                    public override object? Value { get; set; }
                    public override void ResetDbType()
                    {
                    }
                }
            }

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
