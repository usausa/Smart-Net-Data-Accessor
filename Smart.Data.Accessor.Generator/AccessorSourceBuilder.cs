namespace Smart.Data.Accessor.Generator;

using System.Globalization;
using System.Text;

using Smart.Data.Accessor.Generator.Models;
using Smart.Data.Accessor.GeneratorShared;

using SourceGenerateHelper;

internal static class AccessorSourceBuilder
{
    /// <summary>
    /// Inspects a method parameter type for spec §7.9 enum-default handling. Returns the FQN of
    /// the enum's underlying primitive (used for an explicit cast at the AddInParameter call) and a
    /// flag indicating whether the parameter is <c>Nullable&lt;TEnum&gt;</c>. Returns <c>(null, false)</c>
    /// for non-enum parameters.
    /// </summary>
    /// <summary>
    /// Builds the value expression passed to <c>AddInParameter</c>/<c>AddInOutParameter</c>. For
    /// enum parameters this emits an explicit cast to the underlying primitive (or its
    /// <c>Nullable&lt;T&gt;</c> for <c>TEnum?</c>) so the runtime <c>Convert.ChangeType</c> in
    /// <c>AssignValue</c> is avoided (spec §7.9).
    /// </summary>
    // 改善2 (P8): a [TypeHandler<>] INPUT parameter binds through the converter-sharing overload
    // AddInParameter<TConverter, TDb, TClr>(cmd, name, value) — the helper calls ToDb + handles null.
    // Non-converter parameters use the plain AddInParameter with a gen-time value expression.
    private static (string Method, string Value) BuildInParameterCall(ParameterModel p)
        => p.ConverterTypeFullName is { } conv
            ? (CodeExpressionHelper.AddInParameterConverter(conv, p.ConverterDbTypeFullName!, p.ConverterClrTypeFullName!), p.Name)
            : ("AddInParameter", BuildParameterValueExpr(p));

    private static string BuildParameterValueExpr(ParameterModel p)
    {
        // spec §7.4 / §7.7: a bound [TypeHandler<>] writes the value via TConverter.ToDb(...) and
        // takes priority over the enum default cast. For Nullable<TClr> a HasValue guard passes the
        // non-null value to ToDb and a null (→ DBNull) otherwise.
        if (p.ConverterTypeFullName is { } converter)
        {
            return p.ConverterValueIsNullable
                ? $"({p.Name}.HasValue ? (object?){converter}.ToDb({p.Name}.Value) : null)"
                : $"{converter}.ToDb({p.Name})";
        }
        if (p.EnumUnderlyingFullName is null)
        {
            return p.Name;
        }
        return CodeExpressionHelper.EnumCastValue(p.EnumUnderlyingFullName, p.IsNullableEnum, p.Name);
    }

    // spec §5.6: the input value expression for a POCO property — {argName}.{property}, with the
    // §7.9 enum-underlying cast when the property is an enum.
    private static string BuildPocoValueExpr(string argName, PocoBindProperty pp)
    {
        var access = argName + "." + pp.PropertyName;
        // spec §7.4 / §7.7: a converter writes the input via TConverter.ToDb (priority over enum cast).
        if (pp.ConverterTypeFullName is { } converter)
        {
            return pp.ConverterValueIsNullable
                ? $"({access}.HasValue ? (object?){converter}.ToDb({access}.Value) : null)"
                : $"{converter}.ToDb({access})";
        }
        if (pp.EnumUnderlyingFullName is null)
        {
            return access;
        }
        return CodeExpressionHelper.EnumCastValue(pp.EnumUnderlyingFullName, pp.IsNullableEnum, access);
    }

    // 改善2 (P8): a [TypeHandler<>] INPUT POCO property binds through AddInParameter<TConverter,TDb,TClr>
    // (the helper calls ToDb + handles null); non-converter properties use the gen-time value expression.
    private static (string Method, string Value) BuildPocoInParameterCall(string argName, PocoBindProperty pp)
        => pp.ConverterTypeFullName is { } conv
            ? (CodeExpressionHelper.AddInParameterConverter(conv, pp.ConverterDbTypeFullName!, pp.ConverterClrTypeFullName!), argName + "." + pp.PropertyName)
            : ("AddInParameter", BuildPocoValueExpr(argName, pp));

    // spec §5.6: emit Add*Parameter for one expanded POCO property (procedure / DirectSql setup).
    private static void EmitPocoPropertyParameter(SourceBuilder builder, char bindMarker, string argName, PocoBindProperty pp)
    {
        var paramName = bindMarker + pp.ParamName;
        var valueExpr = BuildPocoValueExpr(argName, pp);
        var dbTypeExprOrDefault = pp.DbTypeExpr ?? "global::System.Data.DbType.Object";
        var sizeArg = pp.Size is { } sz ? ", " + sz.ToString(CultureInfo.InvariantCulture) : string.Empty;

        switch (pp.Direction)
        {
            case ParameterDirectionKindLegacy.Output:
                builder.Indent().Append(pp.HandleName)
                    .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                    .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                break;
            case ParameterDirectionKindLegacy.InputOutput:
                builder.Indent().Append(pp.HandleName)
                    .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                    .Append(paramName).Append("\", ").Append(valueExpr).Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                break;
            default:
                var (pocoMethod, pocoValue) = BuildPocoInParameterCall(argName, pp);
                builder.Indent()
                    .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(pocoMethod).Append("(cmd, \"")
                    .Append(paramName).Append("\", ").Append(pocoValue).Append(CodeExpressionHelper.DbTypeSizeArgs(pp.DbTypeExpr, pp.Size)).Append(");").NewLine();
                break;
        }
    }

    // spec §7.4 / §7.7: builds the scalar read expression for an [ExecuteScalar] method.
    // Without a converter: ConvertScalar<TClr>(executeCall). With one: read the DB value as TDb and
    // convert via TConverter.FromDb (the [return:] / method / class / profile scope chain).
    private static string BuildScalarReadExpr(MethodModel m, string executeCall)
    {
        const string convertScalar = "global::Smart.Data.Accessor.Helpers.ExecuteHelper.ConvertScalar<";
        if (m.ScalarConverterTypeFullName is { } converter)
        {
            return $"{converter}.FromDb({convertScalar}{m.ScalarConverterDbTypeFullName}>({executeCall})!)";
        }
        return $"{convertScalar}{m.ScalarTypeFullName}>({executeCall})";
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

        // spec §1.4 F12 / §6.3: aggregate /*!helper */ / /*!using */ across all methods,
        // dedupe by (IsStatic, Name), and emit before the namespace declaration.
        // `using static` directives come after plain `using` to match conventional ordering.
        var aggregated = model.Methods
            .SelectMany(m => m.Usings)
            .Distinct()
            .OrderBy(u => u.IsStatic ? 1 : 0)
            .ThenBy(u => u.Name, StringComparer.Ordinal)
            .ToList();
        if (aggregated.Count > 0)
        {
            foreach (var u in aggregated)
            {
                builder.Indent()
                    .Append(u.IsStatic ? "using static " : "using ")
                    .Append(u.Name)
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

        foreach (var m in model.Methods)
        {
            builder.NewLine();
            EmitMethod(builder, m, model.ProviderName);
        }

        builder.EndScope();
        return builder.ToString();
    }

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

        // Pattern B injection — depends on [Provider]:
        //   no  [Provider] → IDbProvider           (single source, calls dbProvider.CreateConnection())
        //   has [Provider] → IDbProviderSelector   (multi-source, calls providerSelector.GetProvider("name").CreateConnection())
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
        if (hasProvider)
        {
            builder.Indent().Append(multiProvider
                ? "this.providerSelector = providerSelector;"
                : "this.dbProvider = dbProvider;").NewLine();
        }
        foreach (var inject in model.Injects)
        {
            builder.Indent().Append("this.").Append(inject.Name).Append(" = ").Append(inject.Name).Append(";").NewLine();
        }
        builder.EndScope();
    }

    private static bool IsAsyncShape(ReturnShapeLegacy s) =>
        s is ReturnShapeLegacy.Task or ReturnShapeLegacy.TaskScalar or ReturnShapeLegacy.TaskList
          or ReturnShapeLegacy.ValueTask or ReturnShapeLegacy.ValueTaskScalar or ReturnShapeLegacy.AsyncEnumerable
          or ReturnShapeLegacy.TaskReader or ReturnShapeLegacy.ValueTaskReader;

    private static bool IsReaderShape(ReturnShapeLegacy s) =>
        s is ReturnShapeLegacy.Reader or ReturnShapeLegacy.TaskReader or ReturnShapeLegacy.ValueTaskReader;

    // Emit each line of `content` (assumed to be '\n'-separated, with no leading
    // indentation) at the SourceBuilder's current IndentLevel. Blank lines round-trip
    // as `NewLine()` without an indent prefix.
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

    private static void EmitMethod(SourceBuilder builder, MethodModel m, string? providerName)
    {
        // Per-method OrdinalCache struct (spec §7.10.4). Cached once per query, reused per row.
        EmitOrdinalCacheStruct(builder, m);

        var paramList = String.Join(", ", m.Parameters.Select(p =>
        {
            var modifier = p.RefKind switch
            {
                RefKindLegacy.Out => "out ",
                RefKindLegacy.Ref => "ref ",
                _ => string.Empty
            };
            return $"{modifier}{p.TypeFullName} {p.Name}";
        }));
        var isAsync = IsAsyncShape(m.ReturnShapeLegacy);
        var isReader = IsReaderShape(m.ReturnShapeLegacy);
        var asyncKw = isAsync ? "async " : string.Empty;
        builder.Indent()
            .Append(m.Accessibility.ToText()).Append(" ").Append(asyncKw).Append("partial ").Append(m.ReturnTypeFullName).Append(" ")
            .Append(m.Name).Append("(").Append(paramList).Append(")").NewLine();
        builder.BeginScope();

        // Cancellation token discovery
        var ct = m.Parameters.FirstOrDefault(p => p.IsCancellationToken);
        var ctExpr = ct?.Name ?? "default";

        // For reader shapes (ExecuteReader), cmd and (Pattern B) connection ownership
        // is transferred to WrappedReader, so we avoid `using` and add catch/dispose for safety.
        var cmdKeyword = isReader ? "var" : "using var";
        var ownsConnectionForReader = isReader && (m.ConnectionPattern == ConnectionPatternLegacy.None);

        // Pattern A / Pattern B connection acquisition.
        string commandSource;
        switch (m.ConnectionPattern)
        {
            case ConnectionPatternLegacy.ConnectionArg:
            {
                var connName = m.ConnectionParameterName!;
                if (isReader)
                {
                    builder.Indent().Append("var __wasClosed = (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed);").NewLine();
                    if (isAsync)
                    {
                        builder.Indent().Append("if (__wasClosed) await ").Append(connName).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    }
                    else
                    {
                        builder.Indent().Append("if (__wasClosed) ").Append(connName).Append(".Open();").NewLine();
                    }
                }
                else if (isAsync)
                {
                    builder.Indent().Append("if (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed) await ").Append(connName).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("if (").Append(connName).Append(".State == global::System.Data.ConnectionState.Closed) ").Append(connName).Append(".Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = ").Append(connName).Append(".CreateCommand();").NewLine();
                commandSource = connName;
                break;
            }
            case ConnectionPatternLegacy.TransactionArg:
            {
                var txName = m.TransactionParameterName!;
                var connExpr = $"{txName}.Connection!";
                if (isReader)
                {
                    builder.Indent().Append("var __wasClosed = (").Append(connExpr).Append(".State == global::System.Data.ConnectionState.Closed);").NewLine();
                    if (isAsync)
                    {
                        builder.Indent().Append("if (__wasClosed) await ").Append(connExpr).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    }
                    else
                    {
                        builder.Indent().Append("if (__wasClosed) ").Append(connExpr).Append(".Open();").NewLine();
                    }
                }
                else if (isAsync)
                {
                    builder.Indent().Append("if (").Append(connExpr).Append(".State == global::System.Data.ConnectionState.Closed) await ").Append(connExpr).Append(".OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                }
                else
                {
                    builder.Indent().Append("if (").Append(connExpr).Append(".State == global::System.Data.ConnectionState.Closed) ").Append(connExpr).Append(".Open();").NewLine();
                }
                builder.Indent().Append(cmdKeyword).Append(" cmd = ").Append(connExpr).Append(".CreateCommand();").NewLine();
                builder.Indent().Append("cmd.Transaction = ").Append(txName).Append(";").NewLine();
                commandSource = connExpr;
                break;
            }
            default:
            {
                // Pattern B: connection comes from the injected provider.
                //   no  [Provider] → `this.dbProvider.CreateConnection()`
                //   has [Provider] → `this.providerSelector.GetProvider("name").CreateConnection()`
                var providerCallExpr = providerName is null
                    ? "this.dbProvider.CreateConnection()"
                    : $"this.providerSelector.GetProvider(\"{providerName.Replace("\"", "\\\"")}\").CreateConnection()";
                var connKeyword = isReader ? "var" : "using var";
                builder.Indent().Append(connKeyword).Append(" connection = ").Append(providerCallExpr).Append(";").NewLine();
                if (isAsync)
                {
                    builder.Indent().Append("await connection.OpenAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
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
            // Reader shapes: wrap from cmd usage through WrappedReader return in try/catch
            // so cmd (and connection for Pattern B) is disposed if anything throws before
            // ownership transfers to WrappedReader.
            builder.Indent().Append("try").NewLine();
            builder.BeginScope();
        }

        if (m.CommandTimeoutSeconds is { } cts)
        {
            builder.Indent().Append("cmd.CommandTimeout = ").Append(cts.ToString(CultureInfo.InvariantCulture)).Append(";").NewLine();
        }

        // SQL / parameter setup
        if (m.MethodKind == "DirectSql")
        {
            EmitDirectSqlSetup(builder, m);
        }
        else if (m.ProcedureName is not null)
        {
            EmitProcedureSetup(builder, m);
        }
        else if (m.BuilderMethodName is not null)
        {
            builder.Indent().Append("var ctx = new global::Smart.Data.Accessor.Builders.BuilderContext(cmd);").NewLine();
            // design doc §4.3: value parameters = method params excluding DbConnection / DbTransaction / CancellationToken.
            // Both generators must apply the identical exclusion so the call and the generated
            // `{Method}__QueryBuilder` signature line up.
            var valueArgs = m.Parameters
                .Where(p => !p.IsCancellationToken && !p.IsDbConnection && !p.IsDbTransaction)
                .Select(p => p.Name);
            var args = String.Join(", ", new[] { "ref ctx" }.Concat(valueArgs));
            builder.Indent().Append(m.BuilderMethodName).Append("(").Append(args).Append(");").NewLine();
        }
        else
        {
            // Pre-declare OUT / InOut / ReturnValue parameter handles so they remain accessible
            // after the SQL-building try/finally block.
            foreach (var binding in m.OutputBindings)
            {
                builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
            }

            if (m.StaticSqlText is not null)
            {
                // Static SQL fast path: no dynamic branches → emit literal CommandText
                // and parameter setup without StringBuilderPool / try-finally.
                builder.Indent().Append("cmd.CommandText = \"").Append(EscapeCSharpString(m.StaticSqlText)).Append("\";").NewLine();
                if (!String.IsNullOrEmpty(m.StaticParameterCode))
                {
                    AppendCodeLines(builder, m.StaticParameterCode);
                }
            }
            else
            {
                // Tokenized 2-way SQL → emit StringBuilder build code.
                builder.Indent().Append("var __sb = global::Smart.Data.Accessor.Helpers.StringBuilderPool.Rent();").NewLine();
                builder.Indent().Append("try").NewLine();
                builder.BeginScope();
                if (!String.IsNullOrEmpty(m.SqlEmitCode))
                {
                    AppendCodeLines(builder, m.SqlEmitCode);
                }
                builder.Indent().Append("cmd.CommandText = __sb.ToString();").NewLine();
                builder.EndScope();
                builder.Indent().Append("finally").NewLine();
                builder.BeginScope();
                builder.Indent().Append("global::Smart.Data.Accessor.Helpers.StringBuilderPool.Return(__sb);").NewLine();
                builder.EndScope();
            }
        }

        EmitInvocation(builder, m, ctExpr);

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

    private static void EmitDirectSqlSetup(SourceBuilder builder, MethodModel m)
    {
        if (m.DirectSqlParameterName is null)
        {
            builder.Indent().Append("// [DirectSql] could not locate a string parameter to use as SQL source.").NewLine();
            return;
        }

        builder.Indent().Append("cmd.CommandText = ").Append(m.DirectSqlParameterName).Append(";").NewLine();

        // X1 / spec §1.4 F14: pre-declare OUT / InOut handles so EmitOutputWriteback can
        // read them after the execute call.
        foreach (var binding in m.OutputBindings)
        {
            builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
        }

        foreach (var p in m.Parameters)
        {
            if (p.IsCancellationToken || p.IsDbConnection || p.IsDbTransaction)
            {
                continue;
            }
            if (p.Name == m.DirectSqlParameterName)
            {
                continue;
            }
            if (p.PocoProperties is { } pocoProps)
            {
                // spec §5.6: expand the POCO argument into one parameter per property.
                foreach (var pp in pocoProps)
                {
                    EmitPocoPropertyParameter(builder, m.BindMarker, p.Name, pp);
                }
                continue;
            }

            var paramName = m.BindMarker + p.Name;
            var dbTypeExprOrDefault = p.DbTypeExpr ?? "global::System.Data.DbType.Object";
            var sizeArg = p.Size is { } sz ? ", " + sz.ToString(CultureInfo.InvariantCulture) : string.Empty;
            var hasProvider = p.ProviderParameterTypeFullName is not null;

            switch (p.Direction)
            {
                case ParameterDirectionKindLegacy.Output:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.InputOutput:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p))
                        .Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.ReturnValue:
                    // SDA0210 already reported in BuildAccessorModel; skip emission.
                    break;
                default:
                    if (hasProvider)
                    {
                        var providerSizeArg = p.Size is { } iSz
                            ? ", size: " + iSz.ToString(CultureInfo.InvariantCulture)
                            : string.Empty;
                        var (inMethod, inValue) = BuildInParameterCall(p);
                        builder.Indent()
                            .Append("((").Append(p.ProviderParameterTypeFullName!)
                            .Append(")global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue).Append(providerSizeArg)
                            .Append(")).").Append(p.ProviderPropertyName!).Append(" = ").Append(p.ProviderValueExpr!).Append(";").NewLine();
                    }
                    else
                    {
                        var (inMethod, inValue) = BuildInParameterCall(p);
                        builder.Indent()
                            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue)
                            .Append(CodeExpressionHelper.DbTypeSizeArgs(p.DbTypeExpr, p.Size)).Append(");").NewLine();
                    }
                    break;
            }
        }
    }

    private static void EmitProviderDbTypeAssignment(SourceBuilder builder, ParameterModel p, string handleName)
    {
        if ((p.ProviderParameterTypeFullName is null) || (p.ProviderPropertyName is null) || (p.ProviderValueExpr is null))
        {
            return;
        }
        builder.Indent()
            .Append("((").Append(p.ProviderParameterTypeFullName).Append(")").Append(handleName)
            .Append(").").Append(p.ProviderPropertyName).Append(" = ").Append(p.ProviderValueExpr).Append(";").NewLine();
    }

    private static void EmitProcedureSetup(SourceBuilder builder, MethodModel m)
    {
        var procName = m.ProcedureName!.Replace("\"", "\\\"");
        builder.Indent().Append("cmd.CommandType = global::System.Data.CommandType.StoredProcedure;").NewLine();
        builder.Indent().Append("cmd.CommandText = \"").Append(procName).Append("\";").NewLine();

        // Pre-declare OUT / InOut / ReturnValue parameter handles so they are accessible after Execute.
        foreach (var binding in m.OutputBindings)
        {
            builder.Indent().Append("global::System.Data.Common.DbParameter ").Append(binding.HandleName).Append(" = null!;").NewLine();
        }

        // Emit Add*Parameter for each method parameter, using BindMarker + parameter name.
        foreach (var p in m.Parameters)
        {
            if (p.IsCancellationToken || p.IsDbConnection || p.IsDbTransaction)
            {
                continue;
            }
            if (p.PocoProperties is { } pocoProps)
            {
                // spec §5.6: expand the POCO argument into one parameter per property.
                foreach (var pp in pocoProps)
                {
                    EmitPocoPropertyParameter(builder, m.BindMarker, p.Name, pp);
                }
                continue;
            }

            var paramName = m.BindMarker + p.Name;
            var dbTypeExprOrDefault = p.DbTypeExpr ?? "global::System.Data.DbType.Object";
            var sizeArg = p.Size is { } sz ? ", " + sz.ToString(CultureInfo.InvariantCulture) : string.Empty;
            var hasProvider = p.ProviderParameterTypeFullName is not null;

            switch (p.Direction)
            {
                case ParameterDirectionKindLegacy.Output:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.InputOutput:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(BuildParameterValueExpr(p))
                        .Append(", ").Append(dbTypeExprOrDefault).Append(sizeArg).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                case ParameterDirectionKindLegacy.ReturnValue:
                    builder.Indent()
                        .Append("__op_").Append(p.Name)
                        .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddReturnValueParameter(cmd, \"")
                        .Append(paramName).Append("\", ").Append(dbTypeExprOrDefault).Append(");").NewLine();
                    EmitProviderDbTypeAssignment(builder, p, $"__op_{p.Name}");
                    break;
                default:
                    if (hasProvider)
                    {
                        var providerSizeArg = p.Size is { } iSz
                            ? ", size: " + iSz.ToString(CultureInfo.InvariantCulture)
                            : string.Empty;
                        var (inMethod, inValue) = BuildInParameterCall(p);
                        builder.Indent()
                            .Append("((").Append(p.ProviderParameterTypeFullName!)
                            .Append(")global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue).Append(providerSizeArg)
                            .Append(")).").Append(p.ProviderPropertyName!).Append(" = ").Append(p.ProviderValueExpr!).Append(";").NewLine();
                    }
                    else
                    {
                        var (inMethod, inValue) = BuildInParameterCall(p);
                        builder.Indent()
                            .Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.").Append(inMethod).Append("(cmd, \"")
                            .Append(paramName).Append("\", ").Append(inValue)
                            .Append(CodeExpressionHelper.DbTypeSizeArgs(p.DbTypeExpr, p.Size)).Append(");").NewLine();
                    }
                    break;
            }
        }

        if (m.MapsProcedureReturnValue)
        {
            // spec §5.6: capture the stored-procedure RETURN value (mapped to the method return value).
            builder.Indent().Append("var __returnValue = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddReturnValueParameter(cmd, \"")
                .Append(m.BindMarker).Append("__ReturnValue\", global::System.Data.DbType.Int32);").NewLine();
        }
    }

    private static void EmitOutputWriteback(SourceBuilder builder, MethodModel m)
    {
        foreach (var binding in m.OutputBindings)
        {
            // spec §5.6: POCO-argument output property → write the value back into {arg}.{property}.
            // spec §7.4 / §7.7: with a converter, read the OUT value as TDb then TConverter.FromDb.
            if (binding.WritebackTarget is { } target)
            {
                builder.Indent().Append(target).Append(" = ");
                if (binding.ConverterTypeFullName is { } conv)
                {
                    builder.Append(conv).Append(".FromDb(global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<")
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

            var param = m.Parameters.FirstOrDefault(p => p.Name == binding.ParameterName);
            if ((param is null) || (param.RefKind == RefKindLegacy.None))
            {
                continue;
            }
            builder.Indent()
                .Append(binding.ParameterName)
                .Append(" = global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<")
                .Append(param.TypeFullName).Append(">(").Append(binding.HandleName).Append(")!;").NewLine();
        }
    }

    private static void EmitReaderInvocation(SourceBuilder builder, MethodModel m, string ctExpr)
    {
        var ownsConnection = m.ConnectionPattern == ConnectionPatternLegacy.None;
        var isAsync = m.ReturnShapeLegacy is ReturnShapeLegacy.TaskReader or ReturnShapeLegacy.ValueTaskReader;
        var behaviorArg = ownsConnection
            ? string.Empty
            : "__wasClosed ? global::System.Data.CommandBehavior.CloseConnection : global::System.Data.CommandBehavior.Default";

        if (isAsync)
        {
            var asyncArgs = ownsConnection
                ? ctExpr
                : behaviorArg + ", " + ctExpr;
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

    private static void EmitInvocation(SourceBuilder builder, MethodModel m, string ctExpr)
    {
        var hasOutputs = m.OutputBindings.Count > 0;

        if ((m.MethodKind == "ExecuteReader") || IsReaderShape(m.ReturnShapeLegacy))
        {
            EmitReaderInvocation(builder, m, ctExpr);
            return;
        }

        if ((m.MethodKind == "Execute") || (m.MethodKind == "DirectSql"))
        {
            switch (m.ReturnShapeLegacy)
            {
                case ReturnShapeLegacy.Void:
                    builder.Indent().Append("cmd.ExecuteNonQuery();").NewLine();
                    EmitOutputWriteback(builder, m);
                    break;
                case ReturnShapeLegacy.Scalar:
                    if (m.MapsProcedureReturnValue)
                    {
                        // spec §5.6: stored-procedure RETURN value → method return value.
                        builder.Indent().Append("cmd.ExecuteNonQuery();").NewLine();
                        EmitOutputWriteback(builder, m);
                        builder.Indent().Append("return global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<").Append(m.ScalarTypeFullName!).Append(">(__returnValue)!;").NewLine();
                        break;
                    }
                    // int Execute / scalar
                    if (m.ScalarTypeFullName == "int")
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = cmd.ExecuteNonQuery();").NewLine();
                            EmitOutputWriteback(builder, m);
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
                            builder.Indent().Append("var __result = ").Append(BuildScalarReadExpr(m, "cmd.ExecuteScalar()")).Append(";").NewLine();
                            EmitOutputWriteback(builder, m);
                            builder.Indent().Append("return __result!;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return ").Append(BuildScalarReadExpr(m, "cmd.ExecuteScalar()")).Append("!;").NewLine();
                        }
                    }
                    break;
                case ReturnShapeLegacy.Task:
                    builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    EmitOutputWriteback(builder, m);
                    break;
                case ReturnShapeLegacy.TaskScalar:
                case ReturnShapeLegacy.ValueTaskScalar:
                    if (m.MapsProcedureReturnValue)
                    {
                        // spec §5.6: stored-procedure RETURN value → method return value.
                        builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                        EmitOutputWriteback(builder, m);
                        builder.Indent().Append("return global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetOutputValue<").Append(m.ScalarTypeFullName!).Append(">(__returnValue)!;").NewLine();
                        break;
                    }
                    if (m.ScalarTypeFullName == "int")
                    {
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                            EmitOutputWriteback(builder, m);
                            builder.Indent().Append("return __result;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                        }
                    }
                    else
                    {
                        var scalarExecuteAsync = "await cmd.ExecuteScalarAsync(" + ctExpr + ").ConfigureAwait(false)";
                        if (hasOutputs)
                        {
                            builder.Indent().Append("var __result = ").Append(BuildScalarReadExpr(m, scalarExecuteAsync)).Append(";").NewLine();
                            EmitOutputWriteback(builder, m);
                            builder.Indent().Append("return __result!;").NewLine();
                        }
                        else
                        {
                            builder.Indent().Append("return ").Append(BuildScalarReadExpr(m, scalarExecuteAsync)).Append("!;").NewLine();
                        }
                    }
                    break;
                case ReturnShapeLegacy.ValueTask:
                    builder.Indent().Append("await cmd.ExecuteNonQueryAsync(").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                    EmitOutputWriteback(builder, m);
                    break;
                default:
                    builder.Indent().Append("// unsupported Execute shape").NewLine();
                    break;
            }
            return;
        }

        // Query (spec §7.10.4 OrdinalCache + §16.3 type-specific reader methods).
        // Generator inlines the read loop directly (no ExecuteHelper.QueryBuffer / QueryFirstOrDefault
        // call) so the JIT can specialise the row materialisation and avoid per-row delegate dispatch.
        var ordStruct = OrdinalStructName(m);
        var entityBody = BuildEntityCreationBody(m, "__reader", "__o");
        switch (m.ReturnShapeLegacy)
        {
            case ReturnShapeLegacy.List:
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(m.ElementTypeFullName!).Append(">();").NewLine();
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
            case ReturnShapeLegacy.TaskList:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("var __list = new global::System.Collections.Generic.List<").Append(m.ElementTypeFullName!).Append(">();").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("__list.Add(").Append(entityBody).Append(");").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false));").NewLine();
                builder.EndScope();
                builder.Indent().Append("return __list;").NewLine();
                break;
            case ReturnShapeLegacy.IteratorEnumerable:
                // §7.8.1 F13: emit a per-row `yield return` (no buffered list).
                // OrdinalCache is captured once after the first row arrives.
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
            case ReturnShapeLegacy.AsyncEnumerable:
                // §7.8.1 F13: emit `await ReadAsync` + `yield return` directly.
                // The user's CancellationToken parameter must be annotated [EnumeratorCancellation]
                // (SDA0305 warns when missing).
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false))").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("do").NewLine();
                builder.BeginScope();
                builder.Indent().Append("yield return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("while (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false));").NewLine();
                builder.EndScope();
                break;
            case ReturnShapeLegacy.Scalar:
                // QueryFirst-style: return single mapped item, or default! when the reader is empty.
                builder.Indent().Append("using var __reader = cmd.ExecuteReader(global::System.Data.CommandBehavior.SequentialAccess);").NewLine();
                builder.Indent().Append("if (__reader.Read())").NewLine();
                builder.BeginScope();
                builder.Indent().Append("var __o = ").Append(ordStruct).Append(".From(__reader);").NewLine();
                builder.Indent().Append("return ").Append(entityBody).Append(";").NewLine();
                builder.EndScope();
                builder.Indent().Append("return default!;").NewLine();
                break;
            case ReturnShapeLegacy.TaskScalar:
            case ReturnShapeLegacy.ValueTaskScalar:
                builder.Indent().Append("using var __reader = await cmd.ExecuteReaderAsync(global::System.Data.CommandBehavior.SequentialAccess, ").Append(ctExpr).Append(").ConfigureAwait(false);").NewLine();
                builder.Indent().Append("if (await __reader.ReadAsync(").Append(ctExpr).Append(").ConfigureAwait(false))").NewLine();
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

    // Emit either `new T { Prop = ..., ... }` (class/POCO) or `new T(name: ..., ...)`
    // (record primary ctor, spec §7.10.5). Ordinals come from the supplied OrdinalCache
    // variable; column reads use type-specific reader methods (spec §16.3).
    private static string BuildEntityCreationBody(MethodModel m, string readerVar, string ordVar)
    {
        var sb = new StringBuilder();
        var useCtor = m.UseRecordPrimaryConstructor;
        sb.Append("new ").Append(m.ElementTypeFullName).Append(useCtor ? "(" : " { ");
        var cols = m.QueryColumns;
        if (cols is not null)
        {
            var first = true;
            foreach (var col in cols)
            {
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append(col.PropertyName).Append(useCtor ? ": " : " = ");
                if (col.Converter is { } conv)
                {
                    // spec §7.4 / §7.10: read TDb then convert via TConverter.FromDb. The DB NULL
                    // guard mirrors the typed-reader path ([NotNullColumn] opts out).
                    if (!col.SkipNullCheck)
                    {
                        sb.Append(readerVar).Append(".IsDBNull(").Append(ordVar).Append('.').Append(col.PropertyName).Append(')')
                          .Append(" ? default! : ");
                    }
                    sb.Append(conv.ConverterTypeFullName).Append(".FromDb(");
                    if (conv.DbTypedReaderMethod is not null)
                    {
                        sb.Append(readerVar).Append('.').Append(conv.DbTypedReaderMethod).Append('(').Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                    }
                    else
                    {
                        sb.Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetValue<")
                          .Append(conv.DbTypeFullName)
                          .Append(">(").Append(readerVar).Append(", ").Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                    }
                    sb.Append(')');
                }
                else if (col.TypedReaderMethod is not null)
                {
                    if (!col.SkipNullCheck)
                    {
                        // spec §7.10.1: non-nullable property receiving DB NULL falls through as default! (SDA0307).
                        // [NotNullColumn] opts out of this check; provider throws InvalidCastException on actual NULL.
                        sb.Append(readerVar).Append(".IsDBNull(").Append(ordVar).Append('.').Append(col.PropertyName).Append(')')
                          .Append(" ? default! : ");
                    }
                    if (col.EnumCastTypeFullName is not null)
                    {
                        // spec §7.9 / §7.10.3: Enum is read as its underlying primitive then cast back.
                        // For unsigned / sbyte underlyings an intermediate bit-preserving cast bridges
                        // the signed reader result, e.g. (MyEnum)(uint)reader.GetInt32(ord).
                        sb.Append('(').Append(col.EnumCastTypeFullName).Append(')');
                        if (col.EnumUnderlyingCastFullName is not null)
                        {
                            sb.Append('(').Append(col.EnumUnderlyingCastFullName).Append(')');
                        }
                    }
                    sb.Append(readerVar).Append('.').Append(col.TypedReaderMethod).Append('(').Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                }
                else
                {
                    sb.Append("global::Smart.Data.Accessor.Helpers.ExecuteHelper.GetValue<")
                      .Append(col.TypeFullName)
                      .Append(">(").Append(readerVar).Append(", ").Append(ordVar).Append('.').Append(col.PropertyName).Append(')');
                }
            }
        }
        sb.Append(useCtor ? ")" : " }");
        return sb.ToString();
    }

    private static string OrdinalStructName(MethodModel m) => "__" + m.Name + "Ordinals";

    private static void EmitOrdinalCacheStruct(SourceBuilder builder, MethodModel m)
    {
        if ((m.QueryColumns is not { } cols) || (cols.Count == 0))
        {
            return;
        }

        var name = OrdinalStructName(m);
        builder.Indent().Append("private readonly struct ").Append(name).NewLine();
        builder.BeginScope();
        foreach (var col in cols)
        {
            builder.Indent().Append("public readonly int ").Append(col.PropertyName).Append(";").NewLine();
        }
        builder.NewLine();
        var ctorParams = String.Join(", ", cols.Select(c => "int " + LowerFirst(c.PropertyName)));
        builder.Indent().Append("private ").Append(name).Append("(").Append(ctorParams).Append(")").NewLine();
        builder.BeginScope();
        foreach (var col in cols)
        {
            builder.Indent().Append(col.PropertyName).Append(" = ").Append(LowerFirst(col.PropertyName)).Append(";").NewLine();
        }
        builder.EndScope();
        builder.NewLine();
        builder.Indent().Append("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]").NewLine();
        builder.Indent().Append("public static ").Append(name).Append(" From(global::System.Data.Common.DbDataReader reader)").NewLine();
        builder.IndentLevel++;
        builder.Indent().Append("=> new(");
        for (var i = 0; i < cols.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            var col = cols[i];
            builder.Append("reader.GetOrdinal(\"").Append(col.ColumnName).Append("\")");
        }
        builder.Append(");").NewLine();
        builder.IndentLevel--;
        builder.EndScope();
    }

    private static string LowerFirst(string s) =>
        String.IsNullOrEmpty(s) || Char.IsLower(s[0]) ? s : Char.ToLowerInvariant(s[0]) + s[1..];

    private static string EscapeCSharpString(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
