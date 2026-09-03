namespace Smart.Data.Accessor.Generator.Tests;

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
        // 17 グループ(閾値超え)＝サンプリングハッシュ switch 形。閾値以下の直比較形は
        // NarrowEntityOrdinalResolutionUsesDirectComparison で検証する。
        // Seventeen groups (above the threshold) = the sampling-hash switch form; the at-or-below-threshold
        // direct-comparison form is covered by NarrowEntityOrdinalResolutionUsesDirectComparison.
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
                public int Rank { get; set; }
                public int Level { get; set; }
                public string Note { get; set; } = string.Empty;
                public string City { get; set; } = string.Empty;
                public string Country { get; set; } = string.Empty;
                public int Version { get; set; }
                public int OwnerId { get; set; }
                public int GroupId { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from T")).AllGeneratedText;

        // __From はリーダー列を 1 回走査し、__Match(列名 → グループ id)で大小無視の照合を行う(SQL 識別子と同じ扱い、
        // 先勝ち)。__Match は長さ＋サンプリング 3 文字のハッシュで switch し、case 内の String.Equals で確定する。
        // 欠落列は -1 のまま(GetOrdinal は使わない＝欠落列で throw しない)。全グループ解決後は走査を打ち切る。
        // __From scans the reader's columns once and matches case-insensitively through __Match (column name → group
        // id; SQL-identifier-like, first match wins). __Match switches on a length + 3-sampled-character hash and
        // confirms with String.Equals inside the case. An absent column stays -1 (GetOrdinal, which throws on a
        // missing column, is not used). The scan stops once every group is resolved.
        Assert.Contains("private static int __Match(string name)", text, StringComparison.Ordinal);
        Assert.Contains("var __length = name.Length;", text, StringComparison.Ordinal);
        Assert.Contains("switch ((__length << 16) ^ (global::System.Char.ToUpperInvariant(name[", text, StringComparison.Ordinal);
        Assert.Contains("global::System.StringComparison.OrdinalIgnoreCase) ? 0 : -1;", text, StringComparison.Ordinal);
        Assert.Contains("default: return -1;", text, StringComparison.Ordinal);
        Assert.Contains("stackalloc int[17]", text, StringComparison.Ordinal);
        Assert.Contains("var __index = __Match(reader.GetName(__i));", text, StringComparison.Ordinal);
        Assert.Contains("if ((__index >= 0) && (__ordinals[__index] < 0))", text, StringComparison.Ordinal);
        Assert.Contains("if (__resolved == 17) break;", text, StringComparison.Ordinal);
        // 空の列名(プロバイダが無名式列に返すことがある)で name[0] が throw しないためのガード。
        // Guards name[0] against the empty column name a provider can return for an unnamed expression.
        Assert.Contains("if (__length == 0) return -1;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrdinal", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
        // 行マッパーは存在する列(序数 >= 0)だけプロパティへ設定する(無い列は初期値を保持)。
        // The row mapper assigns only columns present in the result set (ordinal >= 0); absent columns keep their defaults.
        Assert.Contains("if (o.Id >= 0) entity.Id =", text, StringComparison.Ordinal);
        Assert.Contains("if (o.Name >= 0) entity.Name =", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SamplingPositionSearchAvoidsDefaultCollisions()
    {
        // 同一長＋共通接頭辞＋共通末尾は SQL でありふれた命名で、既定のサンプリング位置(先頭/中央/末尾)では
        // item_code / item_name / item_note / item_type / item_size の 5 個が同じハッシュに落ちる。
        // このキー集合は位置探索で衝突ゼロに解決できるため、バケット化ではなく「探索が効いていること」の検証。
        // 回避不能な衝突のバケット化は HashSwitchBucketsUnavoidableCollisions で検証する。
        // Equal length + shared prefix + shared final character is idiomatic SQL naming, and under the default sampling
        // positions item_code / item_name / item_note / item_type / item_size all hash alike. This key set is fully
        // resolvable by the position search, so this test covers the search working - not bucketing. Bucketing of
        // collisions the search cannot avoid is covered by HashSwitchBucketsUnavoidableCollisions.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                [Name("item_code")] public string ItemCode { get; set; } = string.Empty;
                [Name("item_name")] public string ItemName { get; set; } = string.Empty;
                [Name("item_note")] public string ItemNote { get; set; } = string.Empty;
                [Name("item_type")] public string ItemType { get; set; } = string.Empty;
                [Name("item_size")] public string ItemSize { get; set; } = string.Empty;
                [Name("item_unit")] public string ItemUnit { get; set; } = string.Empty;
                [Name("item_rank")] public string ItemRank { get; set; } = string.Empty;
                [Name("item_desc")] public string ItemDesc { get; set; } = string.Empty;
                [Name("item_memo")] public string ItemMemo { get; set; } = string.Empty;
                [Name("flag_01")] public int Flag01 { get; set; }
                [Name("flag_02")] public int Flag02 { get; set; }
                [Name("flag_11")] public int Flag11 { get; set; }
                [Name("flag_12")] public int Flag12 { get; set; }
                [Name("value_01")] public int Value01 { get; set; }
                [Name("value_02")] public int Value02 { get; set; }
                [Name("value_03")] public int Value03 { get; set; }
                [Name("value_04")] public int Value04 { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from T")).AllGeneratedText;

        // GeneratorTestHelper.Run は生成コードをコンパイルするので、case ラベルが重複していればここで CS0152 になる。
        // GeneratorTestHelper.Run compiles the generated code, so duplicate case labels would fail here as CS0152.
        Assert.Contains("private static int __Match(string name)", text, StringComparison.Ordinal);
        Assert.Contains("stackalloc int[17]", text, StringComparison.Ordinal);
        Assert.Contains("\"item_code\"", text, StringComparison.Ordinal);
        Assert.Contains("\"item_size\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
        // 衝突ゼロに解決できているので、全 case が単一キーの三項演算子形になる。
        // The search resolves this set to zero collisions, so every case is the single-key ternary form.
        Assert.DoesNotContain("OrdinalIgnoreCase)) return ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HashSwitchBucketsUnavoidableCollisions()
    {
        // 長さ 8 では index 1 がどのサンプリング位置候補からも参照されない。そこだけが異なる 2 キー
        // ("t1_value" / "t2_value" のようなテーブル接頭辞付き列名)は、どの三つ組を選んでも必ず衝突する。
        // C# は case ラベルの重複をコンパイルエラー(CS0152)にするため、この 2 キーは 1 つの case にまとめ、
        // case 内の String.Equals 連鎖で解決しなければならない。バケット化は最適化ではなく必須要件。
        // At length 8, index 1 is not reachable from any candidate sampling position, so two keys differing only there
        // (table-prefixed names like "t1_value" / "t2_value") collide under every possible triple. C# rejects duplicate
        // case labels (CS0152), so the two keys must share one case and be separated by an Equals chain inside it.
        // Bucketing is a requirement, not an optimization.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                [Name("t1_value")] public int T1Value { get; set; }
                [Name("t2_value")] public int T2Value { get; set; }
                public long Id { get; set; }
                public string Name { get; set; } = string.Empty;
                public int Age { get; set; }
                public double Score { get; set; }
                public bool Active { get; set; }
                public int Status { get; set; }
                public string Description { get; set; } = string.Empty;
                public int Category { get; set; }
                public string Tag { get; set; } = string.Empty;
                public int Rank { get; set; }
                public int Level { get; set; }
                public string Note { get; set; } = string.Empty;
                public string City { get; set; } = string.Empty;
                public string Country { get; set; } = string.Empty;
                public int Version { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from T")).AllGeneratedText;

        Assert.Contains("private static int __Match(string name)", text, StringComparison.Ordinal);
        // 衝突した 2 キーは三項演算子形ではなく if 連鎖形になり、末尾で -1 に落ちる。
        // The colliding pair is emitted as an if-chain rather than the ternary form, falling through to -1.
        Assert.Contains("if (global::System.String.Equals(name, \"t1_value\", global::System.StringComparison.OrdinalIgnoreCase)) return 0;", text, StringComparison.Ordinal);
        Assert.Contains("if (global::System.String.Equals(name, \"t2_value\", global::System.StringComparison.OrdinalIgnoreCase)) return 1;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
    }

    [Fact]
    public void MixedScriptColumnNamesStillUseHashSwitch()
    {
        // ASCII/非 ASCII 混在キー。user_名前(長さ7)は index 5,6 に、col_дата(長さ8)は index 4..7 に非 ASCII を
        // 持つが、両者とも ASCII 位置(0,1,2,3 等)をサンプリングできる三つ組が存在するため switch 形が選ばれる
        // (全非 ASCII の NonAsciiColumnNamesFallBackToDirectComparison と対をなす)。全キーの長さを互いに
        // 異ならせているので、長さ項によりどの三つ組でも衝突ゼロ＝全 case が単一キーの三項演算子形になる。
        // GeneratorTestHelper.Run は生成コードをコンパイルするため、生成時ハッシュ定数と emit 式の不一致は
        // ここでは検出できないが、形の選択とキー literal の埋め込みは固定できる(値の一致はランタイムテスト
        // MixedScriptColumnNamesResolveViaHashSwitch が担う)。
        // Mixed ASCII / non-ASCII keys. user_名前 (length 7) has non-ASCII at 5,6 and col_дата (length 8) at 4..7,
        // but triples sampling ASCII positions (0,1,2,3 etc.) exist for both, so the switch form is chosen (the
        // counterpart of the all-non-ASCII NonAsciiColumnNamesFallBackToDirectComparison). Every key length is
        // distinct, so the length term guarantees zero collisions under any triple - every case is the single-key
        // ternary form. GeneratorTestHelper.Run compiles the generated code; hash-constant agreement is covered by
        // the runtime test MixedScriptColumnNamesResolveViaHashSwitch.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                [Name("id")] public long Id { get; set; }
                [Name("age")] public int Age { get; set; }
                [Name("city")] public string City { get; set; } = string.Empty;
                [Name("email")] public string Email { get; set; } = string.Empty;
                [Name("status")] public int Status { get; set; }
                [Name("user_名前")] public string UserName { get; set; } = string.Empty;
                [Name("col_дата")] public string ColDate { get; set; } = string.Empty;
                [Name("item_code")] public string ItemCode { get; set; } = string.Empty;
                [Name("created_at")] public string CreatedAt { get; set; } = string.Empty;
                [Name("status_code")] public string StatusCode { get; set; } = string.Empty;
                [Name("display_name")] public string DisplayName { get; set; } = string.Empty;
                [Name("department_id")] public int DepartmentId { get; set; }
                [Name("address_line_1")] public string AddressLine1 { get; set; } = string.Empty;
                [Name("manager_user_id")] public int ManagerUserId { get; set; }
                [Name("organization_cd1")] public string OrganizationCd1 { get; set; } = string.Empty;
                [Name("registration_date")] public string RegistrationDate { get; set; } = string.Empty;
                [Name("last_modified_by_x")] public string LastModifiedByX { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from T")).AllGeneratedText;

        // 17 グループ(閾値超え)＋安全な三つ組が存在するので switch 形。混在キーも case 内 Equals の literal に載る。
        // Seventeen groups (above the threshold) with a safe triple available -> switch form; the mixed keys appear
        // as literals in the in-case Equals.
        Assert.Contains("private static int __Match(string name)", text, StringComparison.Ordinal);
        Assert.Contains("\"user_名前\"", text, StringComparison.Ordinal);
        Assert.Contains("\"col_дата\"", text, StringComparison.Ordinal);
        Assert.Contains("stackalloc int[17]", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
        // 長さが全て異なるため衝突ゼロ＝if 連鎖形のバケットは現れない。
        // Distinct lengths mean zero collisions - no if-chain bucket appears.
        Assert.DoesNotContain("OrdinalIgnoreCase)) return ", text, StringComparison.Ordinal);
    }

    [Fact]
    public void ControlCharacterColumnNamesEmitEscapedLiterals()
    {
        // U+2028(LINE SEPARATOR)等の C# 行終端文字を含む列名が生のままリテラルへ出ると、生成コードが
        // CS1010(定数内の改行)で壊れる。GeneratorTestHelper.Run は生成コードをコンパイルするため、
        // エスケープ漏れはこのテスト自体が throw して検出する。StringLiteral は行終端
        // (U+0085/U+2028/U+2029)と C0 制御文字・U+007F を \uXXXX で emit する(実行時の文字列値は不変)。
        // テストソースは raw string literal なので \u エスケープはここでは展開されず、埋め込みソースの
        // 通常文字列リテラルとして Roslyn が展開する＝属性値には実際の制御文字が入る。
        // A column name carrying a C# line terminator such as U+2028 (LINE SEPARATOR) would, emitted raw, break
        // the generated code with CS1010 (newline in constant). GeneratorTestHelper.Run compiles the generated
        // code, so a missed escape makes this test itself throw. StringLiteral emits line terminators
        // (U+0085/U+2028/U+2029) and C0 control characters / U+007F as \uXXXX (the runtime string value is
        // unchanged). The test source is a raw string literal, so the \u escapes are not processed here; Roslyn
        // expands them while parsing the embedded source's regular string literals - the attribute values carry
        // the real control characters.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                [Name("li\u2028ne")] public long P1 { get; set; }
                [Name("ne\u0085l")] public string P2 { get; set; } = string.Empty;
                [Name("be\u0007ll")] public int P3 { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from T")).AllGeneratedText;

        // 3 グループ(閾値以下)＝直比較連鎖の Equals リテラルに \uXXXX 形で載る。
        // Three groups (below the threshold) = the escapes appear in the direct chain's Equals literals.
        Assert.Contains("\\u2028", text, StringComparison.Ordinal);
        Assert.Contains("\\u0085", text, StringComparison.Ordinal);
        Assert.Contains("\\u0007", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NonAsciiColumnNamesFallBackToDirectComparison()
    {
        // サンプリング位置を ASCII に取れない列名では switch 形の等価性を保証できないため直比較へ落とす。
        // 理由は 2 つ：ハッシュ定数はジェネレータのランタイムで、照合は対象アプリのランタイムで計算されるので
        // ASCII 以外では両者の Char.ToUpperInvariant が一致する保証が無い。加えてサロゲートをサンプリングすると
        // char 単位の ToUpperInvariant がペアを見られず、OrdinalIgnoreCase では等しいのにハッシュが違う組が実在する。
        // A key set with no ASCII-samplable triple falls back to the direct chain: the hash constant is computed by the
        // generator's runtime and the match by the target app's runtime, and outside ASCII the two Char.ToUpperInvariant
        // results are not guaranteed to agree. Sampling a surrogate is worse still - char-wise ToUpperInvariant cannot
        // see the pair, and pairs exist that are OrdinalIgnoreCase-equal yet hash differently.
        const string source = """
            using System.Collections.Generic;
            using System.Data.Common;
            using Smart.Data.Accessor.Attributes;

            internal sealed class Row
            {
                [Name("社員番号")] public int P1 { get; set; }
                [Name("氏名")] public string P2 { get; set; } = string.Empty;
                [Name("氏名カナ")] public string P3 { get; set; } = string.Empty;
                [Name("所属コード")] public string P4 { get; set; } = string.Empty;
                [Name("所属名称")] public string P5 { get; set; } = string.Empty;
                [Name("役職コード")] public string P6 { get; set; } = string.Empty;
                [Name("入社年月日")] public string P7 { get; set; } = string.Empty;
                [Name("退社年月日")] public string P8 { get; set; } = string.Empty;
                [Name("生年月日")] public string P9 { get; set; } = string.Empty;
                [Name("性別区分")] public int P10 { get; set; }
                [Name("郵便番号")] public string P11 { get; set; } = string.Empty;
                [Name("住所1")] public string P12 { get; set; } = string.Empty;
                [Name("住所2")] public string P13 { get; set; } = string.Empty;
                [Name("電話番号")] public string P14 { get; set; } = string.Empty;
                [Name("更新日時")] public string P15 { get; set; } = string.Empty;
                [Name("更新者")] public string P16 { get; set; } = string.Empty;
                [Name("作成日時")] public string P17 { get; set; } = string.Empty;
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from T")).AllGeneratedText;

        // 17 グループで閾値を超えているが、ASCII をサンプリングできないので直比較になる。
        // Seventeen groups is above the threshold, but with no ASCII to sample it stays on direct comparison.
        Assert.Contains("var __ord0 = -1;", text, StringComparison.Ordinal);
        Assert.Contains("if (__resolved == 17) break;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__Match", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ToUpperInvariant", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
    }

    [Fact]
    public void NarrowEntityOrdinalResolutionUsesDirectComparison()
    {
        // 閾値以下の narrow エンティティは String.Equals(OrdinalIgnoreCase) の直比較連鎖で解決する。
        // 打ち切りは解決数カウンタ：以前は一致の度に「自分以外の全グループが解決済みか」を検査していたが、
        // これは 1 ケースあたり N-1 比較 × N ケースに膨らむ。意味論は同一：大小無視・先勝ち・欠落列 -1・
        // 全解決で走査打ち切り。
        // A narrow entity (at or below the threshold) resolves via a direct String.Equals(OrdinalIgnoreCase) chain,
        // stopping on a resolved counter. The previous form checked "is every other group resolved" on each match,
        // which grows to N-1 comparisons per case across N cases. Semantics are identical: case-insensitive, first
        // match wins, absent columns stay -1, and the scan stops on full resolution.
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
        Assert.Contains("var __resolved = 0;", text, StringComparison.Ordinal);
        Assert.Contains("if (__resolved == 2) break;", text, StringComparison.Ordinal);
        Assert.Contains("return new(__ord0, __ord1);", text, StringComparison.Ordinal);
        Assert.DoesNotContain("FrozenDictionary", text, StringComparison.Ordinal);
        Assert.DoesNotContain("stackalloc", text, StringComparison.Ordinal);
        Assert.DoesNotContain("GetOrdinal", text, StringComparison.Ordinal);
        Assert.DoesNotContain("__Match", text, StringComparison.Ordinal);
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
        // struct 内部名(__Match / __From)はフィールド名＝プロパティ名と衝突し得るため、衝突時は連番になる
        // (無対策だと CS0102 の重複定義で生成コードが壊れる)。17 グループ(閾値超え)でハッシュ switch 形の
        // __Match 衝突と __From 衝突を同時に検証する(直比較形の __From 衝突は下のテスト)。
        // Struct-internal names (__Match / __From) can collide with field names (= property names); a collision takes
        // a numeric suffix (otherwise the generated code breaks with duplicate definitions, CS0102). Seventeen groups
        // (above the threshold) verify both the hash-switch-form __Match collision and the __From collision (the
        // direct-comparison __From collision is covered below).
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
                public int Rank { get; set; }
                public int Level { get; set; }
                public string Note { get; set; } = string.Empty;
                public string City { get; set; } = string.Empty;
                public string Country { get; set; } = string.Empty;
                public int Version { get; set; }
                public int OwnerId { get; set; }
                public int GroupId { get; set; }
                public int __Match { get; set; }
                public int __From { get; set; }
            }

            [DataAccessor]
            internal sealed partial class Accessor
            {
                [Query]
                public partial IReadOnlyList<Row> List(DbConnection con);
            }
            """;

        var text = GeneratorTestHelper.Run(source, ("Accessor.List", "select * from T")).AllGeneratedText;

        Assert.Contains("private static int __Match1(string name)", text, StringComparison.Ordinal);
        Assert.Contains("var __index = __Match1(reader.GetName(__i));", text, StringComparison.Ordinal);
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
