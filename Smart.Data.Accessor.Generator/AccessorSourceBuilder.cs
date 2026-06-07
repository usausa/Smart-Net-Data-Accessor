namespace Smart.Data.Accessor.Generator;

using System.Globalization;
using System.Text;

using Smart.Data.Accessor.Generator.Models;
using Smart.Data.Accessor.Shared.Helpers;

using SourceGenerateHelper;

internal static class AccessorSourceBuilder
{
    // 入力パラメータの (AddInParameter 系メソッド名, 値式) を決める。[TypeHandler<>] が付いた入力パラメータは converter 共有
    // オーバーロード AddInParameter<TConverter, TDb, TClr>(cmd, name, value) で束縛する（ヘルパーが ToDb と null を処理）。
    // converter が無いパラメータは通常の AddInParameter ＋ 生成時の値式を使う。
    // Decide the (AddInParameter-family method name, value expression) for an input parameter. A [TypeHandler<>] input
    // parameter binds through the converter-sharing overload AddInParameter<TConverter, TDb, TClr>(cmd, name, value)
    // (the helper calls ToDb + handles null); a non-converter parameter uses the plain AddInParameter with a gen-time value expression.
    private static (string Method, string Value) BuildInParameterCall(ParameterModel parameter)
        => parameter.ConverterTypeFullName is { } converter
            ? (CodeExpressionHelper.AddInParameterConverter(converter, parameter.ConverterDbTypeFullName!, parameter.ConverterClrTypeFullName!), parameter.Name)
            : ("AddInParameter", BuildParameterValueExpression(parameter));

    // 入力値式を組み立てる。束縛された [TypeHandler<>] があれば TConverter.ToDb(...) で値を書き、enum 既定キャストより優先する。
    // Nullable<TClr> の場合は HasValue ガードで、非 null なら値を ToDb へ、null なら null（→ DBNull）を渡す。
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

    // POCO プロパティの入力値式（{argName}.{property}）を組み立てる。プロパティが enum なら underlying へのキャストを付ける。
    // Build the input value expression for a POCO property ({argName}.{property}), adding the enum-underlying cast when the property is an enum.
    private static string BuildPocoValueExpression(string argName, PocoBindProperty property)
    {
        var access = argName + "." + property.PropertyName;
        // converter があれば TConverter.ToDb で入力を書く（enum キャストより優先）。
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

    // [TypeHandler<>] が付いた入力 POCO プロパティは AddInParameter<TConverter,TDb,TClr> で束縛する（ヘルパーが ToDb と null を処理）。
    // converter が無いプロパティは生成時の値式を使う。
    // A [TypeHandler<>] input POCO property binds through AddInParameter<TConverter,TDb,TClr> (the helper calls ToDb +
    // handles null); a non-converter property uses the gen-time value expression.
    private static (string Method, string Value) BuildPocoInParameterCall(string argName, PocoBindProperty property)
        => property.ConverterTypeFullName is { } converter
            ? (CodeExpressionHelper.AddInParameterConverter(converter, property.ConverterDbTypeFullName!, property.ConverterClrTypeFullName!), argName + "." + property.PropertyName)
            : ("AddInParameter", BuildPocoValueExpression(argName, property));

    // 展開した POCO プロパティ 1 つ分の Add*Parameter を出力する（ストアド / DirectSql セットアップ用）。Direction に応じて
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
    // converter 有り＝DB 値を TDb として読み TConverter.FromDb で変換する（[return:] / method / class / profile のスコープ鎖で解決）。
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

        foreach (var method in model.Methods)
        {
            builder.NewLine();
            EmitMethod(builder, method, model.ProviderName);
        }

        builder.EndScope();
        return builder.ToString();
    }

    // アクセサのコンストラクタを生成する。Pattern B（接続を注入）なら IDbProvider / IDbProviderSelector フィールドを、[Inject] があれば
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
        //   [Provider] 無し → IDbProvider（単一ソース。dbProvider.CreateConnection() を呼ぶ）
        //   [Provider] 有り → IDbProviderSelector（マルチソース。providerSelector.GetProvider("name").CreateConnection() を呼ぶ）
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

        // 注入された必須依存（プロバイダ / [Inject]）が null のとき、最初の DB 呼び出しまで遅延させずコンストラクタで即座に失敗させる。
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

    // content（'\n' 区切り・先頭インデント無しを前提）の各行を SourceBuilder の現在のインデントで出力する。空行はインデント無しの NewLine()。
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

    // 1 メソッド分の partial 実装を生成する。OrdinalCache 構造体 → シグネチャ → 接続取得（Pattern A/B）→（reader 形は try/catch で
    // 安全に cmd/接続を破棄）→ SQL・パラメータ準備 → 実行 →（reader 以外は）後始末、の順に出力する。
    // Emit one method's partial implementation in order: the OrdinalCache struct, the signature, connection acquisition
    // (Pattern A/B), (for reader shapes) a try/catch that safely disposes cmd/connection, SQL + parameter setup, the
    // invocation, and (for non-reader shapes) cleanup.
    private static void EmitMethod(SourceBuilder builder, MethodModel method, string? providerName)
    {
        // メソッド毎の OrdinalCache 構造体（列序数を 1 クエリにつき 1 回だけ解決し、各行で再利用する）。
        // Per-method OrdinalCache struct (column ordinals resolved once per query, reused per row).
        EmitOrdinalCacheStruct(builder, method);

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

        // CancellationToken 引数を探す（無ければ default）。
        // Discover the CancellationToken parameter (default when absent).
        var cancellation = method.Parameters.FirstOrDefault(x => x.IsCancellationToken);
        var cancellationExpression = cancellation?.Name ?? "default";

        // reader 形（ExecuteReader）では cmd と（Pattern B の）接続の所有権を WrappedReader へ渡すため `using` を使わず、例外時のみ手動破棄する。
        // For reader shapes (ExecuteReader), ownership of cmd and (Pattern B) the connection transfers to WrappedReader,
        // so we avoid `using` and dispose manually only if something throws.
        var cmdKeyword = isReader ? "var" : "using var";
        var ownsConnectionForReader = isReader && (method.ConnectionPattern == ConnectionPattern.None);

        // Pattern A（引数の conn/tx）／Pattern B（注入プロバイダ）の接続取得。閉じていれば開く。
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
            // reader 形：cmd 使用〜WrappedReader 返却までを try/catch で包み、所有権が移る前に例外が出たら cmd（と Pattern B の接続）を破棄する。
            // Reader shapes: wrap from cmd usage through the WrappedReader return in try/catch so cmd (and, for Pattern B,
            // the connection) is disposed if anything throws before ownership transfers to WrappedReader.
            builder.Indent().Append("try").NewLine();
            builder.BeginScope();
        }

        if (method.CommandTimeoutSeconds is { } cts)
        {
            builder.Indent().Append("cmd.CommandTimeout = ").Append(cts.ToString(CultureInfo.InvariantCulture)).Append(";").NewLine();
        }

        // SQL とパラメータの準備。コマンドソース（DirectSql / ストアド / QueryBuilder / 2-way SQL）で分岐する。
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
                builder.Indent().Append("cmd.CommandText = \"").Append(EscapeCSharpString(method.StaticSqlText)).Append("\";").NewLine();
                if (!String.IsNullOrEmpty(method.StaticParameterCode))
                {
                    AppendCodeLines(builder, method.StaticParameterCode);
                }
            }
            else
            {
                // トークン化した 2-way SQL → StringBuilder で組み立てるコードを出す（プールから借り、finally で返す）。
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

        EmitInvocation(builder, method, cancellationExpression);

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

    // [DirectSql] のセットアップを出力する。第 1 引数（string）を cmd.CommandText に代入し、残りの引数をパラメータとして束縛する
    // （POCO 引数はプロパティ毎に展開、OUT/InOut はハンドル経由）。
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

    // プロバイダ固有 DbType（[DbType<TEnum>]）の設定を出力する。生成したパラメータをプロバイダ固有型へキャストし、固有プロパティに代入する。
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
    // （POCO 展開・OUT/InOut/ReturnValue 対応）。RETURN 値をメソッド戻り値へマップする場合は ReturnValue パラメータを追加する。
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
            // ストアドの RETURN 値を捕捉する（メソッドのスカラー戻り値へマップする）。
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

    // reader（ExecuteReader）系の実行と返却を出力する。cmd/接続を WrappedReader に包んで返す。Pattern A は接続を閉じない
    // （CloseConnection で呼び出し前の状態へ戻す）、Pattern B（接続所有）は WrappedReader が接続ごと破棄する。同期/非同期で出し分ける。
    // Emit execution and return for reader (ExecuteReader) shapes: wrap cmd/connection in a WrappedReader and return it.
    // Pattern A does not close the connection (CloseConnection restores the pre-call state); Pattern B (owns the connection)
    // lets WrappedReader dispose the connection too. Sync and async are emitted separately.
    private static void EmitReaderInvocation(SourceBuilder builder, MethodModel method, string cancellationExpression)
    {
        var ownsConnection = method.ConnectionPattern == ConnectionPattern.None;
        var isAsync = method.ReturnShape is ReturnShape.TaskReader or ReturnShape.ValueTaskReader;
        var behaviorArg = ownsConnection
            ? string.Empty
            : "__wasClosed ? global::System.Data.CommandBehavior.CloseConnection : global::System.Data.CommandBehavior.Default";

        if (isAsync)
        {
            var asyncArgs = ownsConnection
                ? cancellationExpression
                : behaviorArg + ", " + cancellationExpression;
            builder.Indent().Append("var __reader = await cmd.ExecuteReaderAsync(").Append(asyncArgs).Append(").ConfigureAwait(false);").NewLine();
            builder.Indent().Append(ownsConnection
                ? "return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, __reader, connection);"
                : "return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, __reader);").NewLine();
        }
        else if (ownsConnection)
        {
            builder.Indent().Append("return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess), connection);").NewLine();
        }
        else
        {
            builder.Indent().Append("return new global::Smart.Data.Accessor.Helpers.WrappedReader(cmd, cmd.ExecuteReader(").Append(behaviorArg).Append("));").NewLine();
        }
    }

    // メソッドの実行部を出力する。reader 形は EmitReaderInvocation、Execute/DirectSql は戻り値形（void/scalar/Task…）毎に
    // ExecuteNonQuery / ExecuteScalar を出し、Query 形は下のリーダーループ（List / 単一 / yield / async）を生成する。
    // Emit the method's execution: reader shapes go to EmitReaderInvocation; Execute/DirectSql emit ExecuteNonQuery /
    // ExecuteScalar per return shape (void/scalar/Task...); Query shapes generate the reader loop below (List / single / yield / async).
    private static void EmitInvocation(SourceBuilder builder, MethodModel method, string cancellationExpression)
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
                    // int Execute / scalar
                    if (method.ScalarTypeFullName == "int")
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
                    if (method.ScalarTypeFullName == "int")
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

        // Query 形：OrdinalCache ＋ 型別リーダーメソッドを使う。読み取りループを直接インライン展開し（ExecuteHelper の
        // QueryBuffer / QueryFirstOrDefault を呼ばない）、行マテリアライズを JIT が特化でき、行毎のデリゲート呼び出しを避けられるようにする。
        // Query shapes use the OrdinalCache + type-specific reader methods. The generator inlines the read loop directly
        // (no ExecuteHelper.QueryBuffer / QueryFirstOrDefault call) so the JIT can specialise row materialisation and avoid
        // per-row delegate dispatch.
        var ordStruct = OrdinalStructName(method);
        var entityBody = BuildEntityCreationBody(method, "__reader", "__o");
        switch (method.ReturnShape)
        {
            case ReturnShape.List:
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(method.ElementTypeFullName!).Append(">();").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("__list.Add(").Append(entityBody).Append(");").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (__reader.Read());").NewLine();
                builder.EndScope();
                builder.Indent().Append("return __list;").NewLine();
                break;
            case ReturnShape.TaskList:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(method.ElementTypeFullName!).Append(">();").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("__list.Add(").Append(entityBody).Append(");").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false));").NewLine();
                builder.EndScope();
                builder.Indent().Append("return __list;").NewLine();
                break;
            case ReturnShape.IteratorEnumerable:
                // 行毎に yield return を出す（バッファリングしない）。OrdinalCache は最初の行が来た後に 1 回だけ取得する。
                // Emit a per-row `yield return` (no buffered list); OrdinalCache is captured once after the first row arrives.
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("yield return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (__reader.Read());").NewLine();
                builder.EndScope();
                break;
            case ReturnShape.AsyncEnumerable:
                // await ReadAsync ＋ yield return を直接出す。利用者の CancellationToken 引数には [EnumeratorCancellation] が必要（無い場合 SDA0305 で警告）。
                // Emit `await ReadAsync` + `yield return` directly. The user's CancellationToken parameter must be annotated
                // [EnumeratorCancellation] (SDA0305 warns when missing).
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
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
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("return default!;").NewLine();
                break;
            case ReturnShape.TaskScalar:
            case ReturnShape.ValueTaskScalar:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(cancellationExpression).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(cancellationExpression).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("return default!;").NewLine();
                break;
            default:
                builder.Indent().Append("// unsupported Query shape").NewLine();
                break;
        }
    }

    // エンティティ生成式を組み立てる：class/POCO は new T { Prop = ..., ... }、record 主コンストラクタは new T(name: ..., ...)。
    // 序数は渡された OrdinalCache 変数から取り、列読み取りは型別リーダーメソッドを使う。
    // Build the entity-creation expression: new T { Prop = ..., ... } for a class/POCO, or new T(name: ..., ...) for a
    // record primary constructor. Ordinals come from the supplied OrdinalCache variable; column reads use type-specific reader methods.
    private static string BuildEntityCreationBody(MethodModel method, string readerVariable, string ordinal)
    {
        var sb = new StringBuilder();
        var useCtor = method.UseRecordPrimaryConstructor;
        sb.Append("new ").Append(method.ElementTypeFullName).Append(useCtor ? "(" : " { ");
        var columns = method.QueryColumns;
        if (columns is not null)
        {
            var first = true;
            foreach (var column in columns)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append(column.PropertyName).Append(useCtor ? ": " : " = ");
                if (column.Converter is { } converter)
                {
                    // TDb として読み TConverter.FromDb で変換する。DB NULL ガードは型別リーダー経路と同じ（[NotNullColumn] で除外可）。
                    // Read TDb then convert via TConverter.FromDb. The DB NULL guard mirrors the typed-reader path ([NotNullColumn] opts out).
                    if (!column.SkipNullCheck)
                    {
                        sb.Append(readerVariable).Append(".IsDBNull(").Append(ordinal).Append('.').Append(column.PropertyName).Append(')')
                          .Append(" ? default! : ");
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
                        // 非 null 許容プロパティが DB NULL を受けると default! になる（SDA0307）。[NotNullColumn] でこのチェックを外すと、実際の NULL ではプロバイダが InvalidCastException を投げる。
                        // A non-nullable property receiving DB NULL falls through as default! (SDA0307). [NotNullColumn] opts
                        // out of this check; the provider throws InvalidCastException on an actual NULL.
                        sb.Append(readerVariable).Append(".IsDBNull(").Append(ordinal).Append('.').Append(column.PropertyName).Append(')')
                          .Append(" ? default! : ");
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
            }
        }
        sb.Append(useCtor ? ")" : " }");
        return sb.ToString();
    }

    private static string OrdinalStructName(MethodModel method) => "__" + method.Name + "Ordinals";

    // クエリ列の序数キャッシュ構造体（__{Method}Ordinals）を生成する。各列の序数を public int フィールドに持ち、From(reader) で
    // GetOrdinal を 1 回だけ呼んで構築する（以降は行毎に再利用）。
    // Emit the query-column ordinal cache struct (__{Method}Ordinals): one public int field per column, built by From(reader)
    // which calls GetOrdinal once (reused per row thereafter).
    private static void EmitOrdinalCacheStruct(SourceBuilder builder, MethodModel method)
    {
        if ((method.QueryColumns is not { } columns) || (columns.Count == 0))
        {
            return;
        }

        var name = OrdinalStructName(method);
        builder.Indent().Append("private readonly struct ").Append(name).NewLine();
        builder.BeginScope();
        foreach (var column in columns)
        {
            builder.Indent().Append("public readonly int ").Append(column.PropertyName).Append(";").NewLine();
        }
        builder.NewLine();
        var ctorParams = String.Join(", ", columns.Select(x => "int " + LowerFirst(x.PropertyName)));
        builder.Indent().Append("private ").Append(name).Append("(").Append(ctorParams).Append(")").NewLine();
        builder.BeginScope();
        foreach (var column in columns)
        {
            builder.Indent().Append(column.PropertyName).Append(" = ").Append(LowerFirst(column.PropertyName)).Append(";").NewLine();
        }
        builder.EndScope();
        builder.NewLine();
        builder.Indent().Append("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]").NewLine();
        builder.Indent().Append("public static ").Append(name).Append(" From(global::System.Data.Common.DbDataReader reader)").NewLine();
        builder.IndentLevel++;
        builder.Indent().Append("=> new(");
        for (var i = 0; i < columns.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            var col = columns[i];
            builder.Append("reader.GetOrdinal(\"").Append(col.ColumnName).Append("\")");
        }
        builder.Append(");").NewLine();
        builder.IndentLevel--;
        builder.EndScope();
    }

    private static string LowerFirst(string text) =>
        String.IsNullOrEmpty(text) || Char.IsLower(text[0]) ? text : Char.ToLowerInvariant(text[0]) + text[1..];

    private static string EscapeCSharpString(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
