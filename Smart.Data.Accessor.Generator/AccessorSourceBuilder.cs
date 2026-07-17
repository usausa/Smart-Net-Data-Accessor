namespace Smart.Data.Accessor.Generator;

using System.Globalization;
using System.Text;

using Smart.Data.Accessor.Generator.Models;
using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

internal static class AccessorSourceBuilder
{
    // 入力パラメータの (AddInParameter 系メソッド名, 値式) を決める。[TypeHandler<>] が付いた入力パラメータは converter 共有
    // オーバーロード AddInParameter<TConverter, TDb, TClr>(cmd, name, value) で束縛する(ヘルパーが ToDb と null を処理)。
    // converter が無いパラメータは通常の AddInParameter ＋ 生成時の値式を使う。
    // Decide the (AddInParameter-family method name, value expression) for an input parameter. A [TypeHandler<>] input
    // parameter binds through the converter-sharing overload AddInParameter<TConverter, TDb, TClr>(cmd, name, value)
    // (the helper calls ToDb + handles null); a non-converter parameter uses the plain AddInParameter with a gen-time value expression.
    private static (string Method, string Value) BuildInParameterCall(ParameterModel parameter)
        => parameter.ConverterTypeFullName is { } converter
            ? (CodeExpressionHelper.AddInParameterConverter(converter, parameter.ConverterDbTypeFullName!, parameter.ConverterClrTypeFullName!), parameter.Name)
            : ("AddInParameter", BuildParameterValueExpression(parameter));

    // 入力値式を組み立てる。束縛された [TypeHandler<>] があれば TConverter.ToDb(...) で値を書き、enum 既定キャストより優先する。
    // Nullable<TClr> の場合は HasValue ガードで、非 null なら値を ToDb へ、null なら null(→ DBNull)を渡す。
    // Build the input value expression. A bound [TypeHandler<>] writes the value via TConverter.ToDb(...) and takes
    // priority over the enum default cast. For Nullable<TClr>, a HasValue guard passes the non-null value to ToDb,
    // otherwise null (→ DBNull).
    private static string BuildParameterValueExpression(ParameterModel parameter)
    {
        if (parameter.ConverterTypeFullName is { } converter)
        {
            return parameter.ConverterValueIsNullable
                ? $"({parameter.Name}.HasValue ? (object?){converter}.ToDb({parameter.Name}.Value) : null)"
                : $"{converter}.ToDb({parameter.Name})";
        }
        if (parameter.EnumUnderlyingFullName is null)
        {
            return parameter.Name;
        }
        return CodeExpressionHelper.EnumCastValue(parameter.EnumUnderlyingFullName, parameter.IsNullableEnum, parameter.Name);
    }

    // POCO プロパティの入力値式({argName}.{property})を組み立てる。プロパティが enum なら underlying へのキャストを付ける。
    // Build the input value expression for a POCO property ({argName}.{property}), adding the enum-underlying cast when the property is an enum.
    private static string BuildPocoValueExpression(string argName, PocoBindProperty property)
    {
        var access = argName + "." + property.PropertyName;
        // converter があれば TConverter.ToDb で入力を書く(enum キャストより優先)。
        // A converter writes the input via TConverter.ToDb (priority over the enum cast).
        if (property.ConverterTypeFullName is { } converter)
        {
            return property.ConverterValueIsNullable
                ? $"({access}.HasValue ? (object?){converter}.ToDb({access}.Value) : null)"
                : $"{converter}.ToDb({access})";
        }
        if (property.EnumUnderlyingFullName is null)
        {
            return access;
        }
        return CodeExpressionHelper.EnumCastValue(property.EnumUnderlyingFullName, property.IsNullableEnum, access);
    }

    // [TypeHandler<>] が付いた入力 POCO プロパティは AddInParameter<TConverter,TDb,TClr> で束縛する(ヘルパーが ToDb と null を処理)。
    // converter が無いプロパティは生成時の値式を使う。
    // A [TypeHandler<>] input POCO property binds through AddInParameter<TConverter,TDb,TClr> (the helper calls ToDb +
    // handles null); a non-converter property uses the gen-time value expression.
    private static (string Method, string Value) BuildPocoInParameterCall(string argName, PocoBindProperty property)
        => property.ConverterTypeFullName is { } converter
            ? (CodeExpressionHelper.AddInParameterConverter(converter, property.ConverterDbTypeFullName!, property.ConverterClrTypeFullName!), argName + "." + property.PropertyName)
            : ("AddInParameter", BuildPocoValueExpression(argName, property));

    // 展開した POCO プロパティ 1 つ分の Add*Parameter を出力する(ストアド / DirectSql セットアップ用)。Direction に応じて
    // OUT / InOut / 通常入力を出し分ける。
    // Emit Add*Parameter for one expanded POCO property (procedure / DirectSql setup), choosing OUT / InOut / plain input by Direction.
    private static void EmitPocoPropertyParameter(SourceBuilder builder, char bindMarker, string argName, PocoBindProperty property)
    {
        var paramName = bindMarker + property.ParamName;
        var valueExpression = BuildPocoValueExpression(argName, property);
        var dbTypeExprOrDefault = property.DbTypeExpression ?? "global::System.Data.DbType.Object";
        var sizeArg = property.Size is { } size ? ", " + size.ToString(CultureInfo.InvariantCulture) : string.Empty;

        switch (property.Direction)
        {
            case ParameterDirectionType.Output:
                builder.Indent().Append(property.HandleName)
                    .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                    .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                break;
            case ParameterDirectionType.InputOutput:
                builder.Indent().Append(property.HandleName)
                    .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                    .Append(paramName).Append("\", ").Append(valueExpression).Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                break;
            default:
                var (pocoMethod, pocoValue) = BuildPocoInParameterCall(argName, property);
                builder.Indent()
                    .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(pocoMethod).Append("(cmd, \"")
                    .Append(paramName).Append("\", ").Append(pocoValue).Append(CodeExpressionHelper.DbTypeSizeArgs(property.DbTypeExpression, property.Size)).Append(");").NewLine();
                break;
        }
    }

    // [ExecuteScalar] メソッドのスカラー読み取り式を組み立てる。converter 無し＝ConvertScalar<TClr>(executeCall)。
    // converter 有り＝DB 値を TDb として読み TConverter.FromDb で変換する([return:] / method / class / profile のスコープ鎖で解決)。
    // Build the scalar read expression for an [ExecuteScalar] method. Without a converter: ConvertScalar<TClr>(executeCall).
    // With one: read the DB value as TDb and convert via TConverter.FromDb (resolved over the [return:] / method / class / profile scope chain).
    private static string BuildScalarReadExpression(MethodModel method, string executeCall)
    {
        const string convertScalar = "global::Smart.Data.Accessor.Helpers.ExecuteHelper.ConvertScalar<";
        if (method.ScalarConverterTypeFullName is { } converter)
        {
            return $"{converter}.FromDb({convertScalar}{method.ScalarConverterDbTypeFullName}>({executeCall})!)";
        }
        return $"{convertScalar}{method.ScalarTypeFullName}>({executeCall})";
    }
    //--------------------------------------------------------------------------------
    // Emit
    //--------------------------------------------------------------------------------

    internal static string Emit(AccessorModel model)
    {
        var builder = new SourceBuilder();
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Indent().Append("#pragma warning disable").NewLine();
        builder.NewLine();

        // 全メソッドの /*!helper */ / /*!using */ を集約し、(IsStatic, Name) で重複除去して名前空間宣言の前に出力する。
        // 慣例に合わせ `using static` は通常の `using` の後に並べる。
        // Aggregate /*!helper */ / /*!using */ across all methods, dedupe by (IsStatic, Name), and emit them before the
        // namespace declaration; `using static` directives come after plain `using` to match conventional ordering.
        var aggregated = model.Methods
            .SelectMany(x => x.Usings)
            .Distinct()
            .OrderBy(x => x.IsStatic ? 1 : 0)
            .ThenBy(x => x.Name, StringComparer.Ordinal)
            .ToList();
        if (aggregated.Count > 0)
        {
            foreach (var usingDirective in aggregated)
            {
                builder.Indent()
                    .Append(usingDirective.IsStatic ? "using static " : "using ")
                    .Append(usingDirective.Name)
                    .Append(";")
                    .NewLine();
            }
            builder.NewLine();
        }

        if (!String.IsNullOrEmpty(model.Namespace))
        {
            builder.Namespace(model.Namespace);
            builder.NewLine();
        }
        builder.Indent().Append(model.Accessibility.ToText()).Append(" partial class ").Append(model.ClassName).NewLine();
        builder.BeginScope();
        EmitConstructor(builder, model);

        var (methodMappings, mappingSets) = BuildMethodMappings(model);

        foreach (var set in mappingSets)
        {
            builder.NewLine();
            EmitOrdinalCacheStruct(builder, set.OrdinalsName, set.FromName, set.Template);
            EmitRowMapperMethod(builder, set.OrdinalsName, set.MapperName, set.Template);
        }

        for (var i = 0; i < model.Methods.Count; i++)
        {
            builder.NewLine();
            EmitMethod(builder, model.Methods[i], model.ProviderName, methodMappings[i]);
        }

        builder.EndScope();
        return builder.ToString();
    }

    // Query メソッドが共有する序数 struct／行マッパー／__From の名前と emit 対象テンプレート。
    // The shared ordinal struct / row mapper / __From names and the emit template for Query methods.
    private sealed record MappingSet(string OrdinalsName, string MapperName, string FromName, MethodModel Template);

    // Query メソッドの (要素型 × 列リスト × ctor パス) 毎に序数 struct と行マッパーを 1 度だけ生成して共有する。
    // メソッド毎の複製を排除し、同名オーバーロードでも重複定義（CS0102/CS0111）にならない。名前は要素型の短名から
    // 採り、衝突時は連番を付ける。一意化は短名単位ではなく生成識別子（struct 名・マッパー名）全体で行う：短名単位
    // だと "MapFoo" の struct と "FooOrdinals" のマッパーが共に __MapFooOrdinals になる交差衝突を見逃す。struct 名は
    // 内包フィールド名（＝プロパティ名）とも一致不可（CS0542）。__From はフィールド名と衝突し得るためセット毎に決める。
    // Share one ordinal struct + row mapper per distinct (element type, column list, ctor path) across methods:
    // removes per-method duplication, and same-name overloads no longer produce duplicate definitions
    // (CS0102/CS0111). Names derive from the element type's short name with a numeric suffix on collision, uniqued
    // across ALL generated identifiers (struct + mapper) — per-short-name tracking would miss cross collisions such
    // as entity "MapFoo"'s struct vs entity "FooOrdinals"'s mapper, both "__MapFooOrdinals". A struct name also must
    // not equal a contained field name (CS0542), and __From can collide with a field name, so it is chosen per set.
    private static (MappingSet?[] MethodMappings, List<MappingSet> MappingSets) BuildMethodMappings(AccessorModel model)
    {
        var methodMappings = new MappingSet?[model.Methods.Count];
        var mappingSets = new List<MappingSet>();
        var setByKey = new Dictionary<(string ElementType, bool RecordCtor, EquatableArray<ColumnInfo> Columns), MappingSet>();
        var usedGeneratedNames = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < model.Methods.Count; i++)
        {
            var method = model.Methods[i];
            if ((method.QueryColumns is not { } columns) || (columns.Count == 0))
            {
                continue;
            }
            var key = (method.ElementTypeFullName!, method.UseRecordPrimaryConstructor, columns);
            if (setByKey.TryGetValue(key, out var shared))
            {
                methodMappings[i] = shared;
                continue;
            }
            var shortName = ShortTypeName(method.ElementTypeFullName!);
            var candidate = shortName;
            var suffix = 1;
            string ordinalsName;
            string mapperName;
            while (true)
            {
                ordinalsName = "__" + candidate + "Ordinals";
                mapperName = "__Map" + candidate;
                if (!usedGeneratedNames.Contains(ordinalsName) &&
                    !usedGeneratedNames.Contains(mapperName) &&
                    !ContainsMappedPropertyName(columns, ordinalsName))
                {
                    usedGeneratedNames.Add(ordinalsName);
                    usedGeneratedNames.Add(mapperName);
                    break;
                }
                candidate = shortName + suffix.ToString(CultureInfo.InvariantCulture);
                suffix++;
            }
            var set = new MappingSet(ordinalsName, mapperName, UniqueStructMemberName("__From", columns), method);
            setByKey.Add(key, set);
            mappingSets.Add(set);
            methodMappings[i] = set;
        }
        return (methodMappings, mappingSets);
    }

    // "global::Ns.Sub.DataEntity" → "DataEntity"（生成識別子用の短名）。ジェネリック要素型では型引数リストを
    // 除去してから最終セグメントを取る（型引数内の '.' に LastIndexOf が落ちて "Int32>" 等の不正識別子片を
    // 切り出さないため）。@ エスケープ等の残る非識別子文字も落とし、常に有効な C# 識別子片を返す
    // （閉じたジェネリック同士の一意化は呼び出し側の連番が行う）。
    // "global::Ns.Sub.DataEntity" → "DataEntity" (short name for generated identifiers). For a generic element type
    // the type-argument list is stripped before taking the last segment (LastIndexOf would otherwise land inside a
    // type argument and cut an invalid fragment such as "Int32>"). Remaining non-identifier characters (@ escapes
    // etc.) are dropped so the result is always a valid identifier fragment (closed generics sharing a name are
    // uniqued by the caller's numeric suffix).
    private static string ShortTypeName(string typeFullName)
    {
        var sb = new StringBuilder(typeFullName.Length);
        var depth = 0;
        foreach (var character in typeFullName)
        {
            if (character == '<')
            {
                depth++;
            }
            else if (character == '>')
            {
                depth--;
            }
            else if (depth == 0)
            {
                sb.Append(character);
            }
        }
        var stripped = sb.ToString();
        var index = stripped.LastIndexOf('.');
        var name = index >= 0 ? stripped[(index + 1)..] : stripped;
        if (name.StartsWith("global::", StringComparison.Ordinal))
        {
            name = name["global::".Length..];
        }
        sb.Clear();
        foreach (var character in name)
        {
            if (Char.IsLetterOrDigit(character) || (character == '_'))
            {
                sb.Append(character);
            }
        }
        if ((sb.Length == 0) || Char.IsDigit(sb[0]))
        {
            sb.Insert(0, "Entity");
        }
        return sb.ToString();
    }

    // 短名候補が非 Ignored 列のプロパティ名（＝序数 struct のフィールド名）と一致するかを判定する。
    // Whether a candidate name equals a non-Ignored column's property name (= an ordinal-struct field name).
    private static bool ContainsMappedPropertyName(EquatableArray<ColumnInfo> columns, string name)
    {
        foreach (var column in columns)
        {
            if (!column.Ignored && (column.PropertyName == name))
            {
                return true;
            }
        }
        return false;
    }

    // struct 内部メンバ名（__From / __Columns）はフィールド名＝プロパティ名と衝突し得るため、衝突時は連番を付ける。
    // Struct-internal member names (__From / __Columns) can collide with field names (= property names); append a
    // numeric suffix on collision.
    private static string UniqueStructMemberName(string baseName, EquatableArray<ColumnInfo> columns)
    {
        var name = baseName;
        var suffix = 1;
        while (ContainsMappedPropertyName(columns, name))
        {
            name = baseName + suffix.ToString(CultureInfo.InvariantCulture);
            suffix++;
        }
        return name;
    }

    // アクセサのコンストラクタを生成する。Pattern B(接続を注入)なら IDbProvider / IDbProviderSelector フィールドを、[Inject] があれば
    // 各依存フィールドを持たせ、引数で受けて代入する。Pattern A のみ＆注入無しなら EditorBrowsable(Never) の既定コンストラクタ。
    // Emit the accessor's constructor. For Pattern B (injected connection) it adds an IDbProvider / IDbProviderSelector
    // field, plus a field per [Inject] dependency, taking them as ctor parameters and assigning them. With Pattern A only
    // and no injects, it emits a default constructor marked EditorBrowsable(Never).
    private static void EmitConstructor(SourceBuilder builder, AccessorModel model)
    {
        var hasProvider = model.RequiresConnectionFactory;
        var multiProvider = model.ProviderName is not null;
        var hasInjects = model.Injects.Count > 0;

        if (!hasProvider && !hasInjects)
        {
            builder.Indent().Append("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").NewLine();
            builder.Indent().Append("internal ").Append(model.ClassName).Append("()").NewLine();
            builder.BeginScope();
            builder.EndScope();
            return;
        }

        // Pattern B の注入フィールドは [Provider] 有無で変わる：
        //   [Provider] 無し → IDbProvider(単一ソース。dbProvider.CreateConnection() を呼ぶ)
        //   [Provider] 有り → IDbProviderSelector(マルチソース。providerSelector.GetProvider("name").CreateConnection() を呼ぶ)
        // The Pattern B injection field depends on [Provider]:
        //   no  [Provider] → IDbProvider          (single source; calls dbProvider.CreateConnection())
        //   has [Provider] → IDbProviderSelector  (multi-source; calls providerSelector.GetProvider("name").CreateConnection())
        if (hasProvider)
        {
            if (multiProvider)
            {
                builder.Indent().Append("private readonly global::Smart.Data.IDbProviderSelector providerSelector;").NewLine();
            }
            else
            {
                builder.Indent().Append("private readonly global::Smart.Data.IDbProvider dbProvider;").NewLine();
            }
        }
        foreach (var inject in model.Injects)
        {
            builder.Indent().Append("private readonly ").Append(inject.TypeFullName).Append(" ").Append(inject.Name).Append(";").NewLine();
        }
        builder.NewLine();

        var ctorParams = new List<string>();
        if (hasProvider)
        {
            ctorParams.Add(multiProvider
                ? "global::Smart.Data.IDbProviderSelector providerSelector"
                : "global::Smart.Data.IDbProvider dbProvider");
        }
        foreach (var inject in model.Injects)
        {
            ctorParams.Add($"{inject.TypeFullName} {inject.Name}");
        }

        builder.Indent().Append("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]").NewLine();
        builder.Indent().Append("internal ").Append(model.ClassName).Append("(").Append(String.Join(", ", ctorParams)).Append(")").NewLine();
        builder.BeginScope();

        // 注入された必須依存(プロバイダ / [Inject])が null のとき、最初の DB 呼び出しまで遅延させずコンストラクタで即座に失敗させる。
        // 失敗箇所がアクセサ生成時に固定され、未登録依存の原因を追いやすくなる。
        // Fail fast in the constructor when an injected required dependency (provider / [Inject]) is null, instead of
        // deferring to the first DB call. The failure is pinned to accessor creation, making an unregistered dependency easy to trace.
        var accessorName = String.IsNullOrEmpty(model.Namespace)
            ? model.ClassName
            : model.Namespace + "." + model.ClassName;
        if (hasProvider)
        {
            var providerField = multiProvider ? "providerSelector" : "dbProvider";
            var providerType = multiProvider ? "Smart.Data.IDbProviderSelector" : "Smart.Data.IDbProvider";
            builder.Indent()
                .Append("this.").Append(providerField).Append(" = ").Append(providerField)
                .Append(" ?? throw new global::System.ArgumentNullException(nameof(").Append(providerField)
                .Append("), \"DataAccessor '").Append(accessorName).Append("' requires a registered ").Append(providerType).Append(".\");")
                .NewLine();
        }
        foreach (var inject in model.Injects)
        {
            builder.Indent().Append("global::System.ArgumentNullException.ThrowIfNull(").Append(inject.Name).Append(");").NewLine();
            builder.Indent().Append("this.").Append(inject.Name).Append(" = ").Append(inject.Name).Append(";").NewLine();
        }
        builder.EndScope();
    }

    private static bool IsAsyncShape(ReturnShape shape) =>
        shape is ReturnShape.Task or ReturnShape.TaskScalar or ReturnShape.TaskList
          or ReturnShape.ValueTask or ReturnShape.ValueTaskScalar or ReturnShape.AsyncEnumerable
          or ReturnShape.TaskReader or ReturnShape.ValueTaskReader;

    private static bool IsReaderShape(ReturnShape shape) =>
        shape is ReturnShape.Reader or ReturnShape.TaskReader or ReturnShape.ValueTaskReader;

    // content('\n' 区切り・先頭インデント無しを前提)の各行を SourceBuilder の現在のインデントで出力する。空行はインデント無しの NewLine()。
    // Emit each line of `content` (assumed '\n'-separated with no leading indentation) at the SourceBuilder's current
    // IndentLevel; blank lines round-trip as `NewLine()` without an indent prefix.
    private static void AppendCodeLines(SourceBuilder builder, string? content)
    {
        if (String.IsNullOrEmpty(content))
        {
            return;
        }
        var start = 0;
        for (var i = 0; i < content!.Length; i++)
        {
            if (content[i] == '\n')
            {
                var lineLen = i - start;
                if (lineLen == 0)
                {
                    builder.NewLine();
                }
                else
                {
                    builder.Indent().Append(content.Substring(start, lineLen)).NewLine();
                }
                start = i + 1;
            }
        }
        if (start < content.Length)
        {
            builder.Indent().Append(content[start..]).NewLine();
        }
    }

    // 1 メソッド分の partial 実装を生成する。OrdinalCache 構造体 → シグネチャ → 接続取得(Pattern A/B)→(reader 形は try/catch で
    // 安全に cmd/接続を破棄)→ SQL・パラメータ準備 → 実行 →(reader 以外は)後始末、の順に出力する。
    // Emit one method's partial implementation in order: the OrdinalCache struct, the signature, connection acquisition
    // (Pattern A/B), (for reader shapes) a try/catch that safely disposes cmd/connection, SQL + parameter setup, the
    // invocation, and (for non-reader shapes) cleanup.
    private static void EmitMethod(SourceBuilder builder, MethodModel method, string? providerName, MappingSet? mapping)
    {
        var paramList = String.Join(", ", method.Parameters.Select(x =>
        {
            var modifier = x.RefKind switch
            {
                ParameterRefKind.Out => "out ",
                ParameterRefKind.Ref => "ref ",
                _ => string.Empty
            };
            return $"{modifier}{x.TypeFullName} {x.Name}";
        }));
        var isAsync = IsAsyncShape(method.ReturnShape);
        var isReader = IsReaderShape(method.ReturnShape);
        var asyncKw = isAsync ? "async " : string.Empty;
        builder.Indent()
            .Append(method.Accessibility.ToText()).Append(" ").Append(asyncKw).Append("partial ").Append(method.ReturnTypeFullName).Append(" ")
            .Append(method.Name).Append("(").Append(paramList).Append(")").NewLine();
        builder.BeginScope();

        // CancellationToken 引数を探す(無ければ default)。
        // Discover the CancellationToken parameter (default when absent).
        var cancellation = method.Parameters.FirstOrDefault(x => x.IsCancellationToken);
        var cancellationExpression = cancellation?.Name ?? "default";

        // reader 形(ExecuteReader)では cmd と(Pattern B の)接続の所有権を WrappedReader へ渡すため `using` を使わず、例外時のみ手動破棄する。
        // For reader shapes (ExecuteReader), ownership of cmd and (Pattern B) the connection transfers to WrappedReader,
        // so we avoid `using` and dispose manually only if something throws.
        var cmdKeyword = isReader ? "var" : "using var";
        var ownsConnectionForReader = isReader && (method.ConnectionPattern == ConnectionPattern.None);

        // Pattern A(引数の conn/tx)／Pattern B(注入プロバイダ)の接続取得。閉じていれば開く。
        // Pattern A (conn/tx argument) / Pattern B (injected provider) connection acquisition; opens the connection if closed.
        string commandSource;
        switch (method.ConnectionPattern)
        {
            case ConnectionPattern.ConnectionArg:
            {
                var connName = method.ConnectionParameterName!;
                if (isReader)
                {
                    builder.Indent().Append("var __wasClosed = (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed);").NewLine();
                    if (isAsync)
                    {
                        builder.Indent().Append("if (__wasClosed) await ").Append(connName).Append(".OpenAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                    }
                    else
                    {
                        builder.Indent().Append("if (__wasClosed) ").Append(connName).Append(".Open();").NewLine();
                    }
                }
                else if (isAsync)
                {
                    builder.Indent().Append("if (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed) await ").Append(connName).Append(".OpenAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("if (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed) ").Append(connName).Append(".Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = ").Append(connName).Append(".CreateCommand();").NewLine();
                commandSource = connName;
                break;
            }
            case ConnectionPattern.TransactionArg:
            {
                var txName = method.TransactionParameterName!;
                var connectionExpression = $"{txName}.Connection!";
                if (isReader)
                {
                    builder.Indent().Append("var __wasClosed = (").Append(connectionExpression).Append(".State == global::System.Data.ConnectionState.Closed);").NewLine();
                    if (isAsync)
                    {
                        builder.Indent().Append("if (__wasClosed) await ").Append(connectionExpression).Append(".OpenAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                    }
                    else
                    {
                        builder.Indent().Append("if (__wasClosed) ").Append(connectionExpression).Append(".Open();").NewLine();
                    }
                }
                else if (isAsync)
                {
                    builder.Indent().Append("if (").Append(connectionExpression).Append(".State == global::System.Data.ConnectionState.Closed) await ").Append(connectionExpression).Append(".OpenAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("if (").Append(connectionExpression).Append(".State == global::System.Data.ConnectionState.Closed) ").Append(connectionExpression).Append(".Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = ").Append(connectionExpression).Append(".CreateCommand();").NewLine();
                builder.Indent().Append("cmd.Transaction = ").Append(txName).Append(";").NewLine();
                commandSource = connectionExpression;
                break;
            }
            default:
            {
                // Pattern B：接続は注入プロバイダから取得する。
                //   [Provider] 無し → this.dbProvider.CreateConnection()
                //   [Provider] 有り → this.providerSelector.GetProvider("name").CreateConnection()
                // Pattern B: the connection comes from the injected provider.
                //   no  [Provider] → this.dbProvider.CreateConnection()
                //   has [Provider] → this.providerSelector.GetProvider("name").CreateConnection()
                var providerCallExpression = providerName is null
                    ? "this.dbProvider.CreateConnection()"
                    : $"this.providerSelector.GetProvider(\"{providerName.Replace("\"", "\\\"")}\").CreateConnection()";
                var connKeyword = isReader ? "var" : "using var";
                builder.Indent().Append(connKeyword).Append(" connection = ").Append(providerCallExpression).Append(";").NewLine();
                if (isAsync)
                {
                    builder.Indent().Append("await connection.OpenAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("connection.Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = connection.CreateCommand();").NewLine();
                commandSource = "connection";
                break;
            }
        }
        _ = commandSource;

        if (isReader)
        {
            // reader 形：cmd 使用〜WrappedReader 返却までを try/catch で包み、所有権が移る前に例外が出たら cmd(と Pattern B の接続)を破棄する。
            // Reader shapes: wrap from cmd usage through the WrappedReader return in try/catch so cmd (and, for Pattern B,
            // the connection) is disposed if anything throws before ownership transfers to WrappedReader.
            builder.Indent().Append("try").NewLine();
            builder.BeginScope();
        }

        if (method.CommandTimeoutSeconds is { } cts)
        {
            builder.Indent().Append("cmd.CommandTimeout = ").Append(cts.ToString(CultureInfo.InvariantCulture)).Append(";").NewLine();
        }

        // SQL とパラメータの準備。コマンドソース(DirectSql / ストアド / QueryBuilder / 2-way SQL)で分岐する。
        // SQL and parameter setup, branching on the command source (DirectSql / stored procedure / QueryBuilder / 2-way SQL).
        if (method.SqlSource == SqlSource.DirectSql)
        {
            EmitDirectSqlSetup(builder, method);
        }
        else if (method.ProcedureName is not null)
        {
            EmitProcedureSetup(builder, method);
        }
        else if (method.BuilderMethodName is not null)
        {
            builder.Indent().Append("var context = new global::Smart.Data.Accessor.BuilderContext(cmd);").NewLine();
            // 値パラメータ＝メソッド引数から DbConnection / DbTransaction / CancellationToken を除いたもの。コア・Builder の両ジェネレータが
            // 同一の除外規則を適用しないと、呼び出しと生成される {Method}__QueryBuilder のシグネチャがずれる。
            // Value parameters = method params excluding DbConnection / DbTransaction / CancellationToken. Both generators
            // must apply the identical exclusion so the call and the generated {Method}__QueryBuilder signature line up.
            var valueArgs = method.Parameters
                .Where(x => !x.IsCancellationToken && !x.IsDbConnection && !x.IsDbTransaction)
                .Select(x => x.Name);
            var args = String.Join(", ", new[] { "ref context" }.Concat(valueArgs));
            builder.Indent().Append(method.BuilderMethodName).Append("(").Append(args).Append(");").NewLine();
        }
        else
        {
            // OUT / InOut / ReturnValue のパラメータハンドルを先に宣言し、SQL 組み立ての try/finally を抜けた後も参照できるようにする。
            // Pre-declare OUT / InOut / ReturnValue parameter handles so they remain accessible after the SQL-building try/finally block.
            foreach (var binding in method.OutputBindings)
            {
                builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
            }

            if (method.StaticSqlText is not null)
            {
                // 静的 SQL の高速経路：動的分岐が無いので StringBuilderPool / try-finally を使わず CommandText リテラルとパラメータ設定を直接出す。
                // Static SQL fast path: with no dynamic branches, emit the literal CommandText and parameter setup directly,
                // without StringBuilderPool / try-finally.
                builder.Indent().Append("cmd.CommandText = ").Append(CodeExpressionHelper.StringLiteral(method.StaticSqlText)).Append(";").NewLine();
                if (!String.IsNullOrEmpty(method.StaticParameterCode))
                {
                    AppendCodeLines(builder, method.StaticParameterCode);
                }
            }
            else
            {
                // トークン化した 2-way SQL → StringBuilder で組み立てるコードを出す(プールから借り、finally で返す)。
                // Tokenized 2-way SQL → emit StringBuilder build code (rent from the pool, return it in finally).
                builder.Indent().Append("var __sb = global::Smart.Data.Accessor.Helpers.StringBuilderPool.Rent();").NewLine();
                builder.Indent().Append("try").NewLine();
                builder.BeginScope();
                if (!String.IsNullOrEmpty(method.SqlEmitCode))
                {
                    AppendCodeLines(builder, method.SqlEmitCode);
                }
                builder.Indent().Append("cmd.CommandText = __sb.ToString();").NewLine();
                builder.EndScope();
                builder.Indent().Append("finally").NewLine();
                builder.BeginScope();
                builder.Indent().Append("global::Smart.Data.Accessor.Helpers.StringBuilderPool.Return(__sb);").NewLine();
                builder.EndScope();
            }
        }

        EmitInvocation(builder, method, cancellationExpression, mapping);

        if (isReader)
        {
            builder.EndScope();
            builder.Indent().Append("catch").NewLine();
            builder.BeginScope();
            if (isAsync)
            {
                builder.Indent().Append("await cmd.DisposeAsync().ConfigureAwait(false);").NewLine();
                if (ownsConnectionForReader)
                {
                    builder.Indent().Append("await connection.DisposeAsync().ConfigureAwait(false);").NewLine();
                }
            }
            else
            {
                builder.Indent().Append("cmd.Dispose();").NewLine();
                if (ownsConnectionForReader)
                {
                    builder.Indent().Append("connection.Dispose();").NewLine();
                }
            }
            builder.Indent().Append("throw;").NewLine();
            builder.EndScope();
        }

        builder.EndScope();
    }

    // [DirectSql] のセットアップを出力する。第 1 引数(string)を cmd.CommandText に代入し、残りの引数をパラメータとして束縛する
    // (POCO 引数はプロパティ毎に展開、OUT/InOut はハンドル経由)。
    // Emit the [DirectSql] setup: assign the first (string) argument to cmd.CommandText and bind the remaining arguments
    // as parameters (POCO arguments expand per property; OUT/InOut go through handles).
    private static void EmitDirectSqlSetup(SourceBuilder builder, MethodModel method)
    {
        if (method.DirectSqlParameterName is null)
        {
            builder.Indent().Append("// [DirectSql] could not locate a string parameter to use as SQL source.").NewLine();
            return;
        }

        builder.Indent().Append("cmd.CommandText = ").Append(method.DirectSqlParameterName).Append(";").NewLine();

        // OUT / InOut のハンドルを先に宣言し、実行後に EmitOutputWriteback が読めるようにする。
        // Pre-declare OUT / InOut handles so EmitOutputWriteback can read them after the execute call.
        foreach (var binding in method.OutputBindings)
        {
            builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
        }

        foreach (var parameter in method.Parameters)
        {
            if (parameter.IsCancellationToken || parameter.IsDbConnection || parameter.IsDbTransaction)
            {
                continue;
            }
            if (parameter.Name == method.DirectSqlParameterName)
            {
                continue;
            }
            if (parameter.PocoProperties is { } pocoProps)
            {
                // POCO 引数をプロパティ 1 つにつき 1 パラメータへ展開する。
                // Expand the POCO argument into one parameter per property.
                foreach (var property in pocoProps)
                {
                    EmitPocoPropertyParameter(builder, method.BindMarker, parameter.Name, property);
                }
                continue;
            }

            var paramName = method.BindMarker + parameter.Name;
            var dbTypeExprOrDefault = parameter.DbTypeExpression ?? "global::System.Data.DbType.Object";
            var sizeArg = parameter.Size is { } size ? ", " + size.ToString(CultureInfo.InvariantCulture) : string.Empty;
            var hasProvider = parameter.ProviderParameterTypeFullName is not null;

            switch (parameter.Direction)
            {
                case ParameterDirectionType.Output:
                    builder.Indent()
                        .Append("__op_").Append(parameter.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, parameter, $"__op_{parameter.Name}");
                    break;
                case ParameterDirectionType.InputOutput:
                    builder.Indent()
                        .Append("__op_").Append(parameter.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(BuildParameterValueExpression(parameter))
                        .Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, parameter, $"__op_{parameter.Name}");
                    break;
                case ParameterDirectionType.ReturnValue:
                    // SDA0210 は BuildAccessorModel で報告済みなので、ここでは出力しない。
                    // SDA0210 is already reported in BuildAccessorModel; skip emission here.
                    break;
                default:
                    if (hasProvider)
                    {
                        var providerSizeArg = parameter.Size is { } iSz
                            ? ", size: " + iSz.ToString(CultureInfo.InvariantCulture)
                            : string.Empty;
                        var (inMethod, inValue) = BuildInParameterCall(parameter);
                        builder.Indent()
                            .Append("((").Append(parameter.ProviderParameterTypeFullName!)
                            .Append(")global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue).Append(providerSizeArg)
                            .Append(")).").Append(parameter.ProviderPropertyName!).Append(" = ").Append(parameter.ProviderValueExpression!).Append(";").NewLine();
                    }
                    else
                    {
                        var (inMethod, inValue) = BuildInParameterCall(parameter);
                        builder.Indent()
                            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue)
                            .Append(CodeExpressionHelper.DbTypeSizeArgs(parameter.DbTypeExpression, parameter.Size)).Append(");").NewLine();
                    }
                    break;
            }
        }
    }

    // プロバイダ固有 DbType([DbType<TEnum>])の設定を出力する。生成したパラメータをプロバイダ固有型へキャストし、固有プロパティに代入する。
    // Emit the provider-specific DbType ([DbType<TEnum>]) assignment: cast the created parameter to the provider-specific
    // type and set its native property.
    private static void EmitProviderDbTypeAssignment(SourceBuilder builder, ParameterModel parameter, string handleName)
    {
        if ((parameter.ProviderParameterTypeFullName is null) || (parameter.ProviderPropertyName is null) || (parameter.ProviderValueExpression is null))
        {
            return;
        }
        builder.Indent()
            .Append("((").Append(parameter.ProviderParameterTypeFullName).Append(")").Append(handleName)
            .Append(").").Append(parameter.ProviderPropertyName).Append(" = ").Append(parameter.ProviderValueExpression).Append(";").NewLine();
    }

    // ストアドプロシージャのセットアップを出力する。CommandType=StoredProcedure と手続き名を設定し、各引数をパラメータとして束縛する
    // (POCO 展開・OUT/InOut/ReturnValue 対応)。RETURN 値をメソッド戻り値へマップする場合は ReturnValue パラメータを追加する。
    // Emit the stored-procedure setup: set CommandType=StoredProcedure and the procedure name, then bind each argument as a
    // parameter (POCO expansion, OUT/InOut/ReturnValue). When the RETURN value maps to the method return, add a ReturnValue parameter.
    private static void EmitProcedureSetup(SourceBuilder builder, MethodModel method)
    {
        var procName = method.ProcedureName!.Replace("\"", "\\\"");
        builder.Indent().Append("cmd.CommandType = global::System.Data.CommandType.StoredProcedure;").NewLine();
        builder.Indent().Append("cmd.CommandText = \"").Append(procName).Append("\";").NewLine();

        // Pre-declare OUT / InOut / ReturnValue parameter handles so they are accessible after Execute.
        foreach (var binding in method.OutputBindings)
        {
            builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
        }

        // 各メソッド引数の Add*Parameter を BindMarker ＋ 引数名で出力する。
        // Emit Add*Parameter for each method parameter, using BindMarker + parameter name.
        foreach (var parameter in method.Parameters)
        {
            if (parameter.IsCancellationToken || parameter.IsDbConnection || parameter.IsDbTransaction)
            {
                continue;
            }
            if (parameter.PocoProperties is { } pocoProps)
            {
                // POCO 引数をプロパティ 1 つにつき 1 パラメータへ展開する。
                // Expand the POCO argument into one parameter per property.
                foreach (var property in pocoProps)
                {
                    EmitPocoPropertyParameter(builder, method.BindMarker, parameter.Name, property);
                }
                continue;
            }

            var paramName = method.BindMarker + parameter.Name;
            var dbTypeExprOrDefault = parameter.DbTypeExpression ?? "global::System.Data.DbType.Object";
            var sizeArg = parameter.Size is { } size ? ", " + size.ToString(CultureInfo.InvariantCulture) : string.Empty;
            var hasProvider = parameter.ProviderParameterTypeFullName is not null;

            switch (parameter.Direction)
            {
                case ParameterDirectionType.Output:
                    builder.Indent()
                        .Append("__op_").Append(parameter.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, parameter, $"__op_{parameter.Name}");
                    break;
                case ParameterDirectionType.InputOutput:
                    builder.Indent()
                        .Append("__op_").Append(parameter.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(BuildParameterValueExpression(parameter))
                        .Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, parameter, $"__op_{parameter.Name}");
                    break;
                case ParameterDirectionType.ReturnValue:
                    builder.Indent()
                        .Append("__op_").Append(parameter.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddReturnValueParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, parameter, $"__op_{parameter.Name}");
                    break;
                default:
                    if (hasProvider)
                    {
                        var providerSizeArg = parameter.Size is { } iSz
                            ? ", size: " + iSz.ToString(CultureInfo.InvariantCulture)
                            : string.Empty;
                        var (inMethod, inValue) = BuildInParameterCall(parameter);
                        builder.Indent()
                            .Append("((").Append(parameter.ProviderParameterTypeFullName!)
                            .Append(")global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue).Append(providerSizeArg)
                            .Append(")).").Append(parameter.ProviderPropertyName!).Append(" = ").Append(parameter.ProviderValueExpression!).Append(";").NewLine();
                    }
                    else
                    {
                        var (inMethod, inValue) = BuildInParameterCall(parameter);
                        builder.Indent()
                            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue)
                            .Append(CodeExpressionHelper.DbTypeSizeArgs(parameter.DbTypeExpression, parameter.Size)).Append(");").NewLine();
                    }
                    break;
            }
        }

        if (method.MapsProcedureReturnValue)
        {
            // ストアドの RETURN 値を捕捉する(メソッドのスカラー戻り値へマップする)。
            // Capture the stored-procedure RETURN value (mapped to the method's scalar return value).
            builder.Indent().Append("var __returnValue = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddReturnValueParameter(cmd, \"")
                .Append(method.BindMarker).Append("__ReturnValue\", global::System.Data.DbType.Int32);").NewLine();
        }
    }

    // OUT / InOut / ReturnValue の値を呼び出し側へ書き戻す。POCO 出力プロパティは {arg}.{property} へ、out/ref 引数は引数自身へ代入する。
    // converter があれば OUT 値を TDb として読み TConverter.FromDb で変換する。
    // Write OUT / InOut / ReturnValue values back to the caller: POCO output properties into {arg}.{property}, out/ref
    // parameters into the parameter itself. With a converter, read the OUT value as TDb then TConverter.FromDb.
    private static void EmitOutputWriteback(SourceBuilder builder, MethodModel method)
    {
        foreach (var binding in method.OutputBindings)
        {
            if (binding.WritebackTarget is { } target)
            {
                builder.Indent().Append(target).Append(" = ");
                if (binding.ConverterTypeFullName is { } converter)
                {
                    builder.Append(converter).Append(".FromDb(global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<")
                        .Append(binding.WritebackTypeFullName!).Append(">(").Append(binding.HandleName).Append(")!)");
                }
                else
                {
                    builder.Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<")
                        .Append(binding.WritebackTypeFullName!).Append(">(").Append(binding.HandleName).Append(")!");
                }
                builder.Append(";").NewLine();
                continue;
            }

            var param = method.Parameters.FirstOrDefault(x => x.Name == binding.ParameterName);
            if ((param is null) || (param.RefKind == ParameterRefKind.None))
            {
                continue;
            }
            builder.Indent()
                .Append(binding.ParameterName)
                .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<")
                .Append(param.TypeFullName).Append(">(").Append(binding.HandleName).Append(")!;").NewLine();
        }
    }

    // reader(ExecuteReader)系の実行と返却を出力する。cmd/接続を WrappedReader に包んで返す。Pattern A は接続を閉じない
    // (CloseConnection で呼び出し前の状態へ戻す)、Pattern B(接続所有)は WrappedReader が接続ごと破棄する。同期/非同期で出し分ける。
    // [ReaderBehavior] 指定時は CommandBehavior を合成する：Pattern A は接続状態の三項に OR、Pattern B はそのまま渡す
    // (reader 形は列の読み順を呼出側が制御するため SequentialAccess 等も安全にオプトインできる。Query 形は F17 で固定)。
    // Emit execution and return for reader (ExecuteReader) shapes: wrap cmd/connection in a WrappedReader and return it.
    // Pattern A does not close the connection (CloseConnection restores the pre-call state); Pattern B (owns the connection)
    // lets WrappedReader dispose the connection too. Sync and async are emitted separately. With [ReaderBehavior] the
    // CommandBehavior is composed in: Pattern A ORs it onto the connection-state conditional, Pattern B passes it as-is
    // (the caller controls the column read order for a raw reader, so SequentialAccess etc. are safe opt-ins; Query
    // shapes stay fixed per F17).
    private static void EmitReaderInvocation(SourceBuilder builder, MethodModel method, string cancellationExpression)
    {
        var ownsConnection = method.ConnectionPattern == ConnectionPattern.None;
        var isAsync = method.ReturnShape is ReturnShape.TaskReader or ReturnShape.ValueTaskReader;
        var userBehavior = method.ReaderBehavior is { } behaviorValue ? CommandBehaviorText(behaviorValue) : null;
        var behaviorArg = ownsConnection
            ? userBehavior ?? string.Empty
            : userBehavior is null
                ? "__wasClosed ? global::System.Data.CommandBehavior.CloseConnection : global::System.Data.CommandBehavior.Default"
                : "(__wasClosed ? global::System.Data.CommandBehavior.CloseConnection : global::System.Data.CommandBehavior.Default) | " + userBehavior;

        if (isAsync)
        {
            var asyncArgs = behaviorArg.Length == 0
                ? cancellationExpression
                : behaviorArg + ", " + cancellationExpression;
            builder.Indent().Append("var __reader = await cmd.ExecuteReaderAsync(").Append(asyncArgs).Append(").ConfigureAwait(false);").NewLine();
            builder.Indent().Append(ownsConnection
                ? "return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, __reader, connection);"
                : "return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, __reader);").NewLine();
        }
        else if (ownsConnection)
        {
            builder.Indent().Append("return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, cmd.ExecuteReader(").Append(behaviorArg).Append("), connection);").NewLine();
        }
        else
        {
            builder.Indent().Append("return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, cmd.ExecuteReader(").Append(behaviorArg).Append("));").NewLine();
        }
    }

    // CommandBehavior の基底値を名前付きフラグの OR 式へ分解する(既知外のビットは数値キャストで残す)。
    // Decompose a CommandBehavior underlying value into an OR of named flags (unknown bits fall back to a numeric cast).
    private static readonly (int Flag, string Name)[] CommandBehaviorFlags =
    [
        (1, "SingleResult"),
        (2, "SchemaOnly"),
        (4, "KeyInfo"),
        (8, "SingleRow"),
        (16, "SequentialAccess"),
        (32, "CloseConnection")
    ];

    private static string CommandBehaviorText(int behavior)
    {
        if (behavior == 0)
        {
            return "global::System.Data.CommandBehavior.Default";
        }
        var sb = new StringBuilder();
        var remaining = behavior;
        foreach (var (flag, name) in CommandBehaviorFlags)
        {
            if ((remaining & flag) != 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" | ");
                }
                sb.Append("global::System.Data.CommandBehavior.").Append(name);
                remaining &= ~flag;
            }
        }
        if (remaining != 0)
        {
            if (sb.Length > 0)
            {
                sb.Append(" | ");
            }
            sb.Append("(global::System.Data.CommandBehavior)").Append(remaining.ToString(CultureInfo.InvariantCulture));
        }
        return sb.ToString();
    }

    // メソッドの実行部を出力する。reader 形は EmitReaderInvocation、Execute/DirectSql は戻り値形(void/scalar/Task…)毎に
    // ExecuteNonQuery / ExecuteScalar を出し、Query 形は下のリーダーループ(List / 単一 / yield / async)を生成する。
    // Emit the method's execution: reader shapes go to EmitReaderInvocation; Execute/DirectSql emit ExecuteNonQuery /
    // ExecuteScalar per return shape (void/scalar/Task...); Query shapes generate the reader loop below (List / single / yield / async).
    // Query 形の CommandBehavior（F17）：list/iterator 形は SingleResult、単一行形は SingleResult | SingleRow。
    // CommandBehavior for query shapes (F17): SingleResult for list/iterator shapes, SingleResult | SingleRow for single-row shapes.
    private static string QueryReaderBehavior(bool singleRow) =>
        singleRow
            ? "global::System.Data.CommandBehavior.SingleResult | global::System.Data.CommandBehavior.SingleRow"
            : "global::System.Data.CommandBehavior.SingleResult";

    private static void EmitInvocation(SourceBuilder builder, MethodModel method, string cancellationExpression, MappingSet? mapping)
    {
        var hasOutputs = method.OutputBindings.Count > 0;

        if ((method.MethodType == MethodType.ExecuteReader) || IsReaderShape(method.ReturnShape))
        {
            EmitReaderInvocation(builder, method, cancellationExpression);
            return;
        }

        if ((method.MethodType == MethodType.Execute) || (method.MethodType == MethodType.ExecuteScalar))
        {
            switch (method.ReturnShape)
            {
                case ReturnShape.Void:
                    builder.Indent().Append("cmd.ExecuteNonQuery();").NewLine();
                    EmitOutputWriteback(builder, method);
                    break;
                case ReturnShape.Scalar:
                    if (method.MapsProcedureReturnValue)
                    {
                        // ストアドの RETURN 値 → メソッド戻り値。
                        // Stored-procedure RETURN value -> method return value.
                        builder.Indent().Append("cmd.ExecuteNonQuery();").NewLine();
                        EmitOutputWriteback(builder, method);
                        builder.Indent().Append("return global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<").Append(method.ScalarTypeFullName!).Append(">(__returnValue)!;").NewLine();
                        break;
                    }
                    // [Execute] のスカラー戻り値は影響行数(SDA0302 が int 系へ制限)。[ExecuteScalar] は int を含む任意のスカラーを
                    // ExecuteScalar + ConvertScalar で読む。
                    // An [Execute] scalar return is the affected-row count (SDA0302 restricts it to int shapes).
                    // [ExecuteScalar] reads any scalar (including int) via ExecuteScalar + ConvertScalar.
                    if (method.MethodType == MethodType.Execute)
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = cmd.ExecuteNonQuery();").NewLine();
                            EmitOutputWriteback(builder, method);
                            builder.Indent().Append("return __result;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return cmd.ExecuteNonQuery();").NewLine();
                        }
                    }
                    else
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = ").Append(BuildScalarReadExpression(method, "cmd.ExecuteScalar()")).Append(";").NewLine();
                            EmitOutputWriteback(builder, method);
                            builder.Indent().Append("return __result!;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return ").Append(BuildScalarReadExpression(method, "cmd.ExecuteScalar()")).Append("!;").NewLine();
                        }
                    }
                    break;
                case ReturnShape.Task:
                    builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                    EmitOutputWriteback(builder, method);
                    break;
                case ReturnShape.TaskScalar:
                case ReturnShape.ValueTaskScalar:
                    if (method.MapsProcedureReturnValue)
                    {
                        // ストアドの RETURN 値 → メソッド戻り値。
                        // Stored-procedure RETURN value -> method return value.
                        builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                        EmitOutputWriteback(builder, method);
                        builder.Indent().Append("return global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<").Append(method.ScalarTypeFullName!).Append(">(__returnValue)!;").NewLine();
                        break;
                    }
                    if (method.MethodType == MethodType.Execute)
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = await cmd.ExecuteNonQueryAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                            EmitOutputWriteback(builder, method);
                            builder.Indent().Append("return __result;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return await cmd.ExecuteNonQueryAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                        }
                    }
                    else
                    {
                        var scalarExecuteAsync = "await cmd.ExecuteScalarAsync(" + cancellationExpression + ").ConfigureAwait(false)";
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = ").Append(BuildScalarReadExpression(method, scalarExecuteAsync)).Append(";").NewLine();
                            EmitOutputWriteback(builder, method);
                            builder.Indent().Append("return __result!;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return ").Append(BuildScalarReadExpression(method, scalarExecuteAsync)).Append("!;").NewLine();
                        }
                    }
                    break;
                case ReturnShape.ValueTask:
                    builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                    EmitOutputWriteback(builder, method);
                    break;
                default:
                    builder.Indent().Append("// unsupported Execute shape").NewLine();
                    break;
            }
            return;
        }

        // Query 形：OrdinalCache ＋ 行マッパー(__Map{Method}、AggressiveInlining の static 直呼び)を使い、読み取りループを
        // 直接展開する(ExecuteHelper の QueryBuffer / QueryFirstOrDefault もデリゲートも使わない)。序数は名前照合で解決し
        // 欠落列は -1(部分列 SELECT・動的列を許容。旧 GetOrdinal 方式は欠落列で throw していた)。
        // CommandBehavior は list/iterator 形が SingleResult、単一行形が SingleResult | SingleRow。SequentialAccess は使わない：
        // 列はプロパティ宣言順で読むため ordinal 昇順アクセスを保証できず、SqlClient / Npgsql では実行時例外になり得る。
        // Query shapes use the OrdinalCache + the row mapper (__Map{Method}, an aggressively-inlined static call) with a
        // directly expanded read loop (no ExecuteHelper.QueryBuffer / QueryFirstOrDefault, no delegates). Ordinals resolve
        // by name matching with -1 for absent columns (tolerates subset SELECTs / dynamic columns; the former GetOrdinal
        // form threw on a missing column).
        // CommandBehavior: SingleResult for list/iterator shapes, SingleResult | SingleRow for single-row shapes.
        // SequentialAccess is not used: columns are read in property declaration order, so ascending-ordinal access
        // cannot be guaranteed and SqlClient / Npgsql could throw at runtime.
        var ordinalFactory = mapping!.OrdinalsName + "." + mapping.FromName + "(__reader)";
        var entityBody = mapping.MapperName + "(__reader, in __o)";
        switch (method.ReturnShape)
        {
            case ReturnShape.List:
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(").Append(QueryReaderBehavior(singleRow: false)).Append(");").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(method.ElementTypeFullName!).Append(">();").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordinalFactory).Append(";").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("__list.Add(").Append(entityBody).Append(");").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (__reader.Read());").NewLine();
                builder.EndScope();
                builder.Indent().Append("return __list;").NewLine();
                break;
            case ReturnShape.TaskList:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(").Append(QueryReaderBehavior(singleRow: false)).Append(", ").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(method.ElementTypeFullName!).Append(">();").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordinalFactory).Append(";").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("__list.Add(").Append(entityBody).Append(");").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false));").NewLine();
                builder.EndScope();
                builder.Indent().Append("return __list;").NewLine();
                break;
            case ReturnShape.IteratorEnumerable:
                // 行毎に yield return を出す(バッファリングしない)。OrdinalCache は最初の行が来た後に 1 回だけ取得する。
                // Emit a per-row `yield return` (no buffered list); OrdinalCache is captured once after the first row arrives.
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(").Append(QueryReaderBehavior(singleRow: false)).Append(");").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordinalFactory).Append(";").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("yield return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (__reader.Read());").NewLine();
                builder.EndScope();
                break;
            case ReturnShape.AsyncEnumerable:
                // await ReadAsync ＋ yield return を直接出す。利用者の CancellationToken 引数には [EnumeratorCancellation] が必要(無い場合 SDA0305 で警告)。
                // Emit `await ReadAsync` + `yield return` directly. The user's CancellationToken parameter must be annotated
                // [EnumeratorCancellation] (SDA0305 warns when missing).
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(").Append(QueryReaderBehavior(singleRow: false)).Append(", ").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordinalFactory).Append(";").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("yield return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false));").NewLine();
                builder.EndScope();
                break;
            case ReturnShape.Scalar:
                // QueryFirst スタイル：マップした単一要素を返す。リーダーが空なら default!。
                // QueryFirst-style: return the single mapped item, or default! when the reader is empty.
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(").Append(QueryReaderBehavior(singleRow: true)).Append(");").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordinalFactory).Append(";").NewLine();
                builder.Indent().Append("return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("return default!;").NewLine();
                break;
            case ReturnShape.TaskScalar:
            case ReturnShape.ValueTaskScalar:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(").Append(QueryReaderBehavior(singleRow: true)).Append(", ").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordinalFactory).Append(";").NewLine();
                builder.Indent().Append("return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("return default!;").NewLine();
                break;
            default:
                builder.Indent().Append("// unsupported Query shape").NewLine();
                break;
        }
    }

    // 1 列分の読み取り式(代入・ctor 引数の右辺)を組み立てる。converter(FromDb)／型別リーダー(enum キャスト含む)／
    // GetValue<T> フォールバックの 3 経路。[NotNullColumn] 以外は IsDBNull ガード付き(DB NULL は default(プロパティ型)、SDA0307)。
    // Build one column's read expression (assignment / ctor-argument RHS): converter (FromDb), typed reader (incl. enum
    // casts), or the GetValue<T> fallback. Except for [NotNullColumn], the read carries the IsDBNull guard (DB NULL
    // falls through as a property-typed default, SDA0307).
    private static string BuildColumnReadExpression(ColumnInfo column, string readerVariable, string ordinal)
    {
        var sb = new StringBuilder();
        if (column.Converter is { } converter)
        {
            // TDb として読み TConverter.FromDb で変換する。DB NULL ガードは型別リーダー経路と同じ([NotNullColumn] で除外可)。
            // Read TDb then convert via TConverter.FromDb. The DB NULL guard mirrors the typed-reader path ([NotNullColumn] opts out).
            if (!column.SkipNullCheck)
            {
                sb.Append(readerVariable).Append(".IsDBNull(").Append(ordinal).Append('.').Append(column.PropertyName).Append(')')
                  .Append(" ? default(").Append(column.TypeFullName).Append(")! : ");
            }
            sb.Append(converter.ConverterTypeFullName).Append(".FromDb(");
            if (converter.DbTypedReaderMethod is not null)
            {
                sb.Append(readerVariable).Append('.').Append(converter.DbTypedReaderMethod).Append('(').Append(ordinal).Append('.').Append(column.PropertyName).Append(')');
            }
            else
            {
                sb.Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetValue<")
                  .Append(converter.DbTypeFullName)
                  .Append(">(").Append(readerVariable).Append(", ").Append(ordinal).Append('.').Append(column.PropertyName).Append(')');
            }
            sb.Append(')');
        }
        else if (column.TypedReaderMethod is not null)
        {
            if (!column.SkipNullCheck)
            {
                // 非 null 許容プロパティが DB NULL を受けると default になる(SDA0307)。[NotNullColumn] でこのチェックを外すと、実際の NULL ではプロバイダが InvalidCastException を投げる。
                // default は必ずプロパティ型で型付けする：三項式の自然型は typed アーム側(非 nullable)に決まるため、
                // 素の default! では int? 等の Nullable 値型が null ではなく 0 になってしまう。
                // A non-nullable property receiving DB NULL falls through as default (SDA0307). [NotNullColumn] opts
                // out of this check; the provider throws InvalidCastException on an actual NULL. The default MUST be
                // typed with the property type: the conditional's natural type binds to the typed arm, so a bare
                // default! would materialise DB NULL as 0 (not null) for nullable value types such as int?.
                sb.Append(readerVariable).Append(".IsDBNull(").Append(ordinal).Append('.').Append(column.PropertyName).Append(')')
                  .Append(" ? default(").Append(column.TypeFullName).Append(")! : ");
            }
            if (column.EnumCastTypeFullName is not null)
            {
                // enum は underlying プリミティブとして読んでからキャストし直す。unsigned / sbyte の underlying では符号付きの
                // リーダー結果を橋渡しするためビット保存の中間キャストを挟む。例：(MyEnum)(uint)reader.GetInt32(ordinal)。
                // An enum is read as its underlying primitive then cast back. For unsigned / sbyte underlyings an
                // intermediate bit-preserving cast bridges the signed reader result, e.g. (MyEnum)(uint)reader.GetInt32(ordinal).
                sb.Append('(').Append(column.EnumCastTypeFullName).Append(')');
                if (column.EnumUnderlyingCastFullName is not null)
                {
                    sb.Append('(').Append(column.EnumUnderlyingCastFullName).Append(')');
                }
            }
            sb.Append(readerVariable).Append('.').Append(column.TypedReaderMethod).Append('(').Append(ordinal).Append('.').Append(column.PropertyName).Append(')');
        }
        else
        {
            sb.Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetValue<")
              .Append(column.TypeFullName)
              .Append(">(").Append(readerVariable).Append(", ").Append(ordinal).Append('.').Append(column.PropertyName).Append(')');
        }
        return sb.ToString();
    }

    // 行マッパーメソッド（__Map{Entity}）を生成する。序数キャッシュを受け取り 1 行分のエンティティを構築する。
    // class/POCO：settable プロパティは `new T()` の後、結果セットに存在する列（序数 >= 0）だけを設定する — 無い列は
    // 「設定しない」（プロパティ初期化子・既定値をそのまま保つ）。init-only / required プロパティは初期化子の外で代入
    // できないため `new T { ... }` 内でガード付き三項により設定し、無い列は default(プロパティ型) になる。record 主
    // コンストラクタは全引数が必須のため、無い列は default(引数型)・[Ignore] 引数は default! を渡す（構造上「設定
    // しない」は不可能）。ただし宣言既定値を持つ [Ignore]/プロパティ無し引数は名前付き引数ごと省略して宣言既定値を
    // 生かす。マップ対象外の required メンバ（[Ignore]・非 public・record 非位置）は初期化子で default! を設定する
    // （CS9035 回避）。三項内の default をプロパティ型で型付けするのは、自然型が typed アーム側に決まり素の
    // default! では Nullable 値型が 0 になるため。引数位置の単独 default! は target-typed なのでそのままで正しい。
    // Emit the row-mapper method (__Map{Entity}): takes the ordinal cache and materialises one row. For a class/POCO,
    // settable properties are assigned after `new T()` only when the column is present (ordinal >= 0) — an absent
    // column is NOT assigned (property initialisers / defaults survive). Init-only / required properties cannot be
    // assigned outside an object initialiser, so they are set inside `new T { ... }` with a guarded conditional and
    // receive a property-typed default when absent. A record primary constructor requires every argument, so absent
    // columns pass a parameter-typed default and [Ignore] arguments pass default! (skipping is structurally
    // impossible) — except that an [Ignore] / property-less parameter with a declared default value omits the named
    // argument so the declared default applies. Required members excluded from mapping ([Ignore] / non-public /
    // record non-positional) are set to default! inside the initializer (avoiding CS9035). Defaults inside a
    // conditional MUST be typed — the natural type binds to the typed arm and a bare default! turns nullable value
    // types into 0; a stand-alone default! in argument position is target-typed and safe.
    private static void EmitRowMapperMethod(SourceBuilder builder, string ordinalsName, string mapperName, MethodModel template)
    {
        var columns = template.QueryColumns!.Value;
        builder.Indent().Append("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]").NewLine();
        builder.Indent().Append("private static ").Append(template.ElementTypeFullName!).Append(' ').Append(mapperName)
            .Append("(global::System.Data.Common.DbDataReader reader, in ").Append(ordinalsName).Append(" o)").NewLine();
        builder.BeginScope();
        if (template.UseRecordPrimaryConstructor)
        {
            builder.Indent().Append("return new ").Append(template.ElementTypeFullName!).Append('(');
            var first = true;
            var hasExtraRequired = false;
            foreach (var column in columns)
            {
                // 主 ctor 外（非位置）の required メンバは ctor 引数ではなく後段の初期化子で default! を設定する。
                // A required member outside the primary ctor (non-positional) is set in the trailing initializer, not as a ctor argument.
                if (column.RequiresInitOnlySet)
                {
                    hasExtraRequired = true;
                    continue;
                }
                // 宣言既定値を持つ [Ignore]/プロパティ無し引数は名前付き引数ごと省略し、宣言既定値を生かす。
                // An [Ignore] / property-less parameter with a declared default value omits the named argument so the declared default applies.
                if (column.Ignored && column.HasDefaultValue)
                {
                    continue;
                }
                if (!first)
                {
                    builder.Append(", ");
                }
                first = false;
                builder.Append(column.PropertyName).Append(": ");
                if (column.Ignored)
                {
                    builder.Append("default!");
                }
                else
                {
                    builder.Append("o.").Append(column.PropertyName).Append(" < 0 ? default(").Append(column.TypeFullName).Append(")! : (")
                        .Append(BuildColumnReadExpression(column, "reader", "o")).Append(')');
                }
            }
            builder.Append(')');
            if (hasExtraRequired)
            {
                builder.Append(" { ");
                var firstExtra = true;
                foreach (var column in columns)
                {
                    if (!column.RequiresInitOnlySet)
                    {
                        continue;
                    }
                    if (!firstExtra)
                    {
                        builder.Append(", ");
                    }
                    firstExtra = false;
                    builder.Append(column.PropertyName).Append(" = default!");
                }
                builder.Append(" }");
            }
            builder.Append(';').NewLine();
        }
        else
        {
            var hasInitOnly = false;
            foreach (var column in columns)
            {
                if (column.RequiresInitOnlySet)
                {
                    hasInitOnly = true;
                    break;
                }
            }
            if (hasInitOnly)
            {
                builder.Indent().Append("var entity = new ").Append(template.ElementTypeFullName!).NewLine();
                builder.Indent().Append("{").NewLine();
                builder.IndentLevel++;
                foreach (var column in columns)
                {
                    if (!column.RequiresInitOnlySet)
                    {
                        continue;
                    }
                    // マップ対象外の required（[Ignore]・非 public）は列読み取り無しで default! を設定する（CS9035 回避）。
                    // An unmapped required member ([Ignore] / non-public) receives default! with no column read (avoiding CS9035).
                    if (column.Ignored)
                    {
                        builder.Indent().Append(column.PropertyName).Append(" = default!,").NewLine();
                        continue;
                    }
                    builder.Indent().Append(column.PropertyName).Append(" = o.").Append(column.PropertyName)
                        .Append(" < 0 ? default(").Append(column.TypeFullName).Append(")! : (").Append(BuildColumnReadExpression(column, "reader", "o")).Append("),").NewLine();
                }
                builder.IndentLevel--;
                builder.Indent().Append("};").NewLine();
            }
            else
            {
                builder.Indent().Append("var entity = new ").Append(template.ElementTypeFullName!).Append("();").NewLine();
            }
            foreach (var column in columns)
            {
                if (column.RequiresInitOnlySet)
                {
                    continue;
                }
                builder.Indent().Append("if (o.").Append(column.PropertyName).Append(" >= 0) entity.").Append(column.PropertyName)
                    .Append(" = ").Append(BuildColumnReadExpression(column, "reader", "o")).Append(';').NewLine();
            }
            builder.Indent().Append("return entity;").NewLine();
        }
        builder.EndScope();
    }

    // __From の照合戦略閾値：グループ数がこの値以下なら FrozenDictionary を使わず String.Equals の直比較を emit する
    // （2026-07 PoC 実測：1〜8 グループの全形＝正順/逆順/部分列 wide で直比較が 2.5〜11 倍高速・割当ゼロ、同一長＋
    // 共通接頭辞の縮退最悪ケースでも 3.7 倍勝ち。static 辞書と型初期化子も消える。9 グループ以上は未計測のため
    // FrozenDictionary を維持＝計測済み上限で切る保守的閾値）。
    // Threshold for the __From matching strategy: at or below this group count, emit direct String.Equals comparisons
    // instead of the FrozenDictionary (2026-07 PoC measurements: 2.5-11x faster with zero allocation across every
    // shape — in-order / reversed / subset-in-wide — for 1-8 groups, and still 3.7x ahead in the same-length
    // shared-prefix degenerate worst case; the static dictionary + type initializer disappear too). 9+ groups keep
    // the dictionary form as the unmeasured region (a conservative cut at the measured bound).
    private const int NarrowOrdinalGroupThreshold = 8;

    // クエリ列の序数キャッシュ構造体（__{Entity}Ordinals）を生成する。マップ対象列毎の public int フィールドを持ち、
    // __From(reader) がリーダーの列を 1 回だけ走査して構築する（GetOrdinal は欠落列で throw するため使わない。欠落列は
    // -1 のまま）。照合は SQL の識別子と同様に大文字小文字を区別しない：グループ数が NarrowOrdinalGroupThreshold 以下
    // なら String.Equals(OrdinalIgnoreCase) の直比較＋グループ毎ローカルで解決し、超えるなら事前構築の static
    // FrozenDictionary（OrdinalIgnoreCase、列名 → グループ id）を引いて stackalloc の序数表へ先勝ちで書き込む
    // （2026-07 PoC 実測で ToUpperInvariant switch / Dapper.AOT 式ハッシュ switch より高速・割当ゼロ）。
    // 大小のみ異なる複数プロパティは同じグループ id を共有し双方が同じ序数に束縛される。
    // 全グループ解決後は走査を打ち切る。全半角・かな種は畳み込まない（プロバイダ GetOrdinal の拡張照合より狭い、F18 の制限）。
    // Emit the query-column ordinal cache struct (__{Entity}Ordinals): one public int field per mapped column, built by
    // __From(reader) scanning the reader's columns once (GetOrdinal, which throws on a missing column, is not used; an
    // absent column stays -1). Matching is case-insensitive like SQL identifiers: at or below NarrowOrdinalGroupThreshold
    // groups, direct String.Equals(OrdinalIgnoreCase) comparisons resolve into per-group locals; above it, a prebuilt
    // static FrozenDictionary (OrdinalIgnoreCase, column name → group id) fills a stackalloc ordinal table
    // first-match-wins (2026-07 PoC measurements: faster than the ToUpperInvariant switch / Dapper.AOT-style hash
    // switch, zero allocation). Properties whose names differ only in case share a group id and both bind
    // to the same ordinal. The scan stops once every group is resolved. Width/kana folding is NOT applied (narrower
    // than provider GetOrdinal's extended collation; an F18 limitation).
    private static void EmitOrdinalCacheStruct(SourceBuilder builder, string ordinalsName, string fromName, MethodModel template)
    {
        var columns = template.QueryColumns!.Value;
        var mapped = new List<ColumnInfo>();
        foreach (var column in columns)
        {
            if (!column.Ignored)
            {
                mapped.Add(column);
            }
        }
        // 静的辞書のフィールド名もフィールド名（＝プロパティ名）と衝突し得るためセット毎に決める（__From と同様）。
        // The static dictionary's field name can also collide with a field (= property) name; choose it per set (like __From).
        var columnsFieldName = UniqueStructMemberName("__Columns", columns);

        // 列名（宣言どおりの表記）を OrdinalIgnoreCase でグルーピング。1 回の前進走査（get-or-add）で
        // グループ名リスト（先勝ちの表記・出現順）と列→グループ id の逆引きを同時に構築する。
        // 大小のみ異なる重複列名は同じグループ id を共有する。
        // Group declared column names with OrdinalIgnoreCase: a single forward scan (get-or-add) builds the group
        // name list (first-win spelling, in appearance order) and the column → group id lookup at the same time.
        // Case-variant duplicate column names share a group id.
        var groupNames = new List<string>();
        var groupIndexByColumn = new int[mapped.Count];
        var groupIdByName = new Dictionary<string, int>(mapped.Count, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < mapped.Count; i++)
        {
            var columnName = mapped[i].ColumnName;
            if (!groupIdByName.TryGetValue(columnName, out var groupIndex))
            {
                groupIndex = groupNames.Count;
                groupIdByName.Add(columnName, groupIndex);
                groupNames.Add(columnName);
            }
            groupIndexByColumn[i] = groupIndex;
        }
        var groupCountText = groupNames.Count.ToString(CultureInfo.InvariantCulture);
        var useDirectComparison = groupNames.Count <= NarrowOrdinalGroupThreshold;

        builder.Indent().Append("private readonly struct ").Append(ordinalsName).NewLine();
        builder.BeginScope();
        if (!useDirectComparison)
        {
            builder.Indent().Append("private static readonly global::System.Collections.Frozen.FrozenDictionary<string, int> ").Append(columnsFieldName).Append(" =").NewLine();
            builder.IndentLevel++;
            builder.Indent().Append("global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(").NewLine();
            builder.IndentLevel++;
            builder.Indent().Append("new global::System.Collections.Generic.Dictionary<string, int>(").Append(groupCountText).Append(", global::System.StringComparer.OrdinalIgnoreCase)").NewLine();
            builder.Indent().Append("{").NewLine();
            builder.IndentLevel++;
            for (var groupIndex = 0; groupIndex < groupNames.Count; groupIndex++)
            {
                builder.Indent().Append("[").Append(CodeExpressionHelper.StringLiteral(groupNames[groupIndex])).Append("] = ")
                    .Append(groupIndex.ToString(CultureInfo.InvariantCulture)).Append(",").NewLine();
            }
            builder.IndentLevel--;
            builder.Indent().Append("},").NewLine();
            builder.Indent().Append("global::System.StringComparer.OrdinalIgnoreCase);").NewLine();
            builder.IndentLevel -= 2;
            builder.NewLine();
        }
        foreach (var column in mapped)
        {
            builder.Indent().Append("public readonly int ").Append(column.PropertyName).Append(";").NewLine();
        }
        builder.NewLine();
        // ctor 引数は p{n} 固定。プロパティ名由来の引数名は予約語化・大小のみの重複・自己代入を起こし得る。
        // 代入は this. 修飾：p{n} と同名のプロパティ（＝フィールド p{n}）があってもパラメータに隠蔽されず
        // フィールドへ正しく代入される。
        // Ctor parameters use fixed p{n} names: property-derived names can become keywords, collide when differing
        // only by case, or self-assign. Assignments are this.-qualified so a property named p{n} (= field p{n})
        // still assigns the field instead of the shadowing parameter.
        var ctorParams = String.Join(", ", Enumerable.Range(0, mapped.Count).Select(static x => "int p" + x.ToString(CultureInfo.InvariantCulture)));
        builder.Indent().Append("private ").Append(ordinalsName).Append("(").Append(ctorParams).Append(")").NewLine();
        builder.BeginScope();
        for (var i = 0; i < mapped.Count; i++)
        {
            builder.Indent().Append("this.").Append(mapped[i].PropertyName).Append(" = p").Append(i.ToString(CultureInfo.InvariantCulture)).Append(";").NewLine();
        }
        builder.EndScope();
        builder.NewLine();
        builder.Indent().Append("public static ").Append(ordinalsName).Append(' ').Append(fromName).Append("(global::System.Data.Common.DbDataReader reader)").NewLine();
        builder.BeginScope();
        if (useDirectComparison)
        {
            EmitDirectComparisonFromBody(builder, groupNames, groupIndexByColumn, mapped.Count);
        }
        else
        {
            builder.Indent().Append("global::System.Span<int> __ordinals = stackalloc int[").Append(groupCountText).Append("];").NewLine();
            builder.Indent().Append("__ordinals.Fill(-1);").NewLine();
            builder.Indent().Append("var __resolved = 0;").NewLine();
            builder.Indent().Append("var __fieldCount = reader.FieldCount;").NewLine();
            builder.Indent().Append("for (var __i = 0; __i < __fieldCount; __i++)").NewLine();
            builder.BeginScope();
            builder.Indent().Append("if (").Append(columnsFieldName).Append(".TryGetValue(reader.GetName(__i), out var __index) && (__ordinals[__index] < 0))").NewLine();
            builder.BeginScope();
            builder.Indent().Append("__ordinals[__index] = __i;").NewLine();
            builder.Indent().Append("__resolved++;").NewLine();
            builder.Indent().Append("if (__resolved == ").Append(groupCountText).Append(") break;").NewLine();
            builder.EndScope();
            builder.EndScope();
            builder.Indent().Append("return new(");
            for (var i = 0; i < mapped.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append("__ordinals[").Append(groupIndexByColumn[i].ToString(CultureInfo.InvariantCulture)).Append("]");
            }
            builder.Append(");").NewLine();
        }
        builder.EndScope();
        builder.EndScope();
    }

    // narrow エンティティ（グループ数 <= NarrowOrdinalGroupThreshold）用の __From 本体：グループ毎の int ローカルへ
    // String.Equals(OrdinalIgnoreCase) の直比較で先勝ち解決する。単一グループは一致時に即 break、複数グループは
    // 自分以外が全て解決済みなら break（＝全解決で走査打ち切り、FrozenDictionary 形と同一意味論）。
    // The __From body for narrow entities (groups <= NarrowOrdinalGroupThreshold): resolve first-match-wins into
    // per-group int locals via direct String.Equals(OrdinalIgnoreCase) comparisons. A single group breaks on match;
    // multiple groups break when every other group is already resolved (same stop-on-full-resolution semantics as
    // the FrozenDictionary form).
    private static void EmitDirectComparisonFromBody(SourceBuilder builder, List<string> groupNames, int[] groupIndexByColumn, int mappedCount)
    {
        for (var groupIndex = 0; groupIndex < groupNames.Count; groupIndex++)
        {
            builder.Indent().Append("var __ord").Append(groupIndex.ToString(CultureInfo.InvariantCulture)).Append(" = -1;").NewLine();
        }
        builder.Indent().Append("var __fieldCount = reader.FieldCount;").NewLine();
        builder.Indent().Append("for (var __i = 0; __i < __fieldCount; __i++)").NewLine();
        builder.BeginScope();
        if (groupNames.Count == 1)
        {
            builder.Indent().Append("if (global::System.String.Equals(reader.GetName(__i), ")
                .Append(CodeExpressionHelper.StringLiteral(groupNames[0]))
                .Append(", global::System.StringComparison.OrdinalIgnoreCase))").NewLine();
            builder.BeginScope();
            builder.Indent().Append("__ord0 = __i;").NewLine();
            builder.Indent().Append("break;").NewLine();
            builder.EndScope();
        }
        else
        {
            builder.Indent().Append("var __name = reader.GetName(__i);").NewLine();
            for (var groupIndex = 0; groupIndex < groupNames.Count; groupIndex++)
            {
                var ordinalLocal = "__ord" + groupIndex.ToString(CultureInfo.InvariantCulture);
                builder.Indent().Append(groupIndex == 0 ? "if ((" : "else if ((").Append(ordinalLocal)
                    .Append(" < 0) && global::System.String.Equals(__name, ")
                    .Append(CodeExpressionHelper.StringLiteral(groupNames[groupIndex]))
                    .Append(", global::System.StringComparison.OrdinalIgnoreCase))").NewLine();
                builder.BeginScope();
                builder.Indent().Append(ordinalLocal).Append(" = __i;").NewLine();
                builder.Indent().Append("if (");
                var first = true;
                for (var otherIndex = 0; otherIndex < groupNames.Count; otherIndex++)
                {
                    if (otherIndex == groupIndex)
                    {
                        continue;
                    }
                    if (!first)
                    {
                        builder.Append(" && ");
                    }
                    first = false;
                    builder.Append("(__ord").Append(otherIndex.ToString(CultureInfo.InvariantCulture)).Append(" >= 0)");
                }
                builder.Append(") break;").NewLine();
                builder.EndScope();
            }
        }
        builder.EndScope();
        builder.Indent().Append("return new(");
        for (var i = 0; i < mappedCount; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            builder.Append("__ord").Append(groupIndexByColumn[i].ToString(CultureInfo.InvariantCulture));
        }
        builder.Append(");").NewLine();
    }
}
