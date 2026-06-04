namespace Smart.Data.Accessor.Generator.Sql;

using System.Globalization;
using System.Text;

using Smart.Data.Accessor.Generator.Sql.Nodes;
using Smart.Data.Accessor.GeneratorShared;

internal static class NodeEmitter
{
    public sealed class EmitResult
    {
        /// <summary>
        /// SQL build code lines (StringBuilderPool path), one statement per line, with no
        /// leading indentation. The caller indents each line via SourceBuilder at its
        /// current IndentLevel.
        /// </summary>
        public string Code { get; }

        /// <summary>
        /// Non-null when the SQL has no dynamic branches (no CodeNode / RawSqlNode and
        /// no multi-value /*@ list */ parameter). In that case the literal SQL text is
        /// pre-built at code-gen time and emitted as `cmd.CommandText = "..."`,
        /// bypassing <c>StringBuilderPool.Rent</c> / <c>Return</c> at runtime.
        /// </summary>
        public string? StaticSqlText { get; }

        /// <summary>
        /// Parameter setup code lines used when <see cref="StaticSqlText"/> is non-null.
        /// One statement per line, with no leading indentation; caller controls indent.
        /// </summary>
        public string StaticParameterCode { get; }

        public IReadOnlyList<string> UndefinedParameters { get; }

        public bool RequiresIEnumerable { get; }

        public IReadOnlyList<OutputBinding> OutputBindings { get; }

        public EmitResult(
            string code,
            string? staticSqlText,
            string staticParameterCode,
            IReadOnlyList<string> undefined,
            bool requiresIEnumerable,
            IReadOnlyList<OutputBinding> outputBindings)
        {
            Code = code;
            StaticSqlText = staticSqlText;
            StaticParameterCode = staticParameterCode;
            UndefinedParameters = undefined;
            RequiresIEnumerable = requiresIEnumerable;
            OutputBindings = outputBindings;
        }
    }

    /// <summary>Output / InOut / ReturnValue binding captured during emission.
    /// The Generator pre-declares the handle variable before the SQL build try-block
    /// and uses it after Execute to write the parameter value back to out/ref args.</summary>
    public sealed record OutputBinding(string ParameterName, string HandleName, Direction Direction);

    /// <summary>Per-parameter attribute metadata gathered from method parameters.</summary>
    public sealed class ParameterAttributes
    {
        public string? DbTypeExpr { get; init; }   // e.g. "global::System.Data.DbType.AnsiString" or null
        public int? Size { get; init; }
        public Direction Direction { get; init; } = Direction.Input;
        public string? OutputHandleName { get; init; }   // optional local variable name to capture DbParameter handle

        /// <summary>FQN of the enum's underlying primitive when this parameter is an enum (or
        /// <c>Nullable&lt;enum&gt;</c>); used to emit an explicit cast so <c>AssignValue</c>'s
        /// runtime <c>Convert.ChangeType</c> is bypassed (spec §7.9). Only applied when the
        /// SQL marker references the bare parameter name (no member access path).</summary>
        public string? EnumUnderlyingFullName { get; init; }

        public bool IsNullableEnum { get; init; }

        // spec §1.4 F15 / §5.3.1: provider-specific DbType assignment emitted after the
        // AddInParameter/AddOutParameter/AddInOutParameter call.
        public string? ProviderParameterTypeFullName { get; init; }
        public string? ProviderPropertyName { get; init; }
        public string? ProviderValueExpr { get; init; }

        // spec §7.4 / §7.7: non-null when a [TypeHandler<>] applies to this parameter; the bound
        // value is written via TConverter.ToDb(...). Applied only to a bare parameter marker
        // (no member-access path). ConverterValueIsNullable adds a HasValue guard for Nullable<TClr>.
        public string? ConverterTypeFullName { get; init; }
        public bool ConverterValueIsNullable { get; init; }

        // spec §7.7 (改善2): the converter's IValueConverter<TDb, TClr> type-argument FQNs, for emitting
        // ExecuteHelper.AddInParameter<TConverter, TDb, TClr>. Meaningful only with ConverterTypeFullName.
        public string? ConverterDbTypeFullName { get; init; }
        public string? ConverterClrTypeFullName { get; init; }
    }

    public enum Direction
    {
        Input,
        Output,
        InputOutput,
        ReturnValue
    }

    public static EmitResult Emit(IReadOnlyList<NodeBase> nodes, ISet<string> knownParameters)
        => Emit(nodes, knownParameters, _ => null, '@');

    public static EmitResult Emit(
        IReadOnlyList<NodeBase> nodes,
        ISet<string> knownParameters,
        Func<string, ParameterAttributes?> attrLookup)
        => Emit(nodes, knownParameters, attrLookup, '@');

    public static EmitResult Emit(
        IReadOnlyList<NodeBase> nodes,
        ISet<string> knownParameters,
        Func<string, ParameterAttributes?> attrLookup,
        char bindMarker)
    {
        var sb = new StringBuilder();
        var sbStaticParam = new StringBuilder();
        var staticSql = new StringBuilder();
        var hasDynamicSql = false;
        var undefined = new List<string>();
        var requiresIEnumerable = false;
        var outputBindings = new List<OutputBinding>();
        var counter = 0;

        // Emit a parameter-binding line into both buffers. The output is indent-less;
        // the caller indents each line via SourceBuilder at the appropriate IndentLevel.
        void EmitParamLine(string line)
        {
            sb.Append(line).Append('\n');
            sbStaticParam.Append(line).Append('\n');
        }

        foreach (var node in nodes)
        {
            switch (node)
            {
                case SqlNode s:
                    if (!string.IsNullOrEmpty(s.Sql))
                    {
                        sb.Append("__sb.Append(\"").Append(Escape(s.Sql)).Append("\");\n");
                        staticSql.Append(s.Sql);
                    }
                    break;

                case ParameterNode p:
                {
                    var root = ExtractRoot(p.Name);
                    if (!knownParameters.Contains(root))
                    {
                        undefined.Add(p.Name);
                    }
                    var pname = bindMarker + "p" + counter++;
                    var attrs = attrLookup(root);
                    var sizeArg = attrs?.Size is { } sz ? ", " + sz.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    var direction = attrs?.Direction ?? Direction.Input;
                    // Apply the spec §7.4/§7.7 converter (ToDb) or the §7.9 enum-cast only when the
                    // SQL token is the bare parameter (no member access) — for `entity.X` the leaf
                    // type is unknown to this layer. A converter takes priority over the enum cast.
                    var valueExpr = p.Name;
                    var inMethod = "AddInParameter";
                    if (!p.Name.Contains('.'))
                    {
                        if (attrs?.ConverterTypeFullName is { } converter)
                        {
                            // 改善2: bind via the converter-sharing overload; valueExpr stays raw (the
                            // helper calls TConverter.ToDb + handles null/Nullable).
                            inMethod = CodeExpressionHelper.AddInParameterConverter(converter, attrs.ConverterDbTypeFullName!, attrs.ConverterClrTypeFullName!);
                        }
                        else if (attrs?.EnumUnderlyingFullName is { } enumUnderlying)
                        {
                            valueExpr = CodeExpressionHelper.EnumCastValue(enumUnderlying, attrs.IsNullableEnum, p.Name);
                        }
                    }
                    var hasProvider = attrs?.ProviderParameterTypeFullName is not null
                        && attrs.ProviderPropertyName is not null
                        && attrs.ProviderValueExpr is not null;
                    if (p.IsMultiple)
                    {
                        // /*@ list */ expands to a comma-separated placeholder list whose
                        // arity depends on the IEnumerable count -> SQL text is dynamic.
                        hasDynamicSql = true;
                        requiresIEnumerable = true;
                        sb.Append("__sb.Append(global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameters(cmd, \"")
                            .Append(pname)
                            .Append("\", ")
                            .Append(p.Name)
                            .Append(CodeExpressionHelper.DbTypeSizeArgs(attrs?.DbTypeExpr, null))
                            .Append("));\n");
                    }
                    else if (direction == Direction.Input)
                    {
                        sb.Append("__sb.Append(\"").Append(pname).Append("\");\n");
                        staticSql.Append(pname);
                        if (hasProvider)
                        {
                            // Skip the positional DbType? slot — provider-specific assignment
                            // will set the native typed property. Use named arg for size to
                            // keep the call unambiguous.
                            var providerSizeArg = attrs?.Size is { } pSz
                                ? ", size: " + pSz.ToString(CultureInfo.InvariantCulture)
                                : string.Empty;
                            EmitParamLine(
                                $"(({attrs!.ProviderParameterTypeFullName})global::Smart.Data.Accessor.Helpers.ExecuteHelper.{inMethod}(cmd, \"{pname}\", {valueExpr}{providerSizeArg})).{attrs.ProviderPropertyName} = {attrs.ProviderValueExpr};");
                        }
                        else
                        {
                            EmitParamLine(
                                $"global::Smart.Data.Accessor.Helpers.ExecuteHelper.{inMethod}(cmd, \"{pname}\", {valueExpr}{CodeExpressionHelper.DbTypeSizeArgs(attrs?.DbTypeExpr, attrs?.Size)});");
                        }
                    }
                    else
                    {
                        // OUT / InOut / ReturnValue — DbType is required.
                        var dbTypeExpr = attrs?.DbTypeExpr ?? "global::System.Data.DbType.Object";
                        var handle = attrs?.OutputHandleName ?? $"__op_{p.Name}";
                        outputBindings.Add(new OutputBinding(p.Name, handle, direction));
                        sb.Append("__sb.Append(\"").Append(pname).Append("\");\n");
                        staticSql.Append(pname);
                        switch (direction)
                        {
                            case Direction.Output:
                                EmitParamLine(
                                    $"{handle} = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"{pname}\", {dbTypeExpr}{sizeArg});");
                                break;
                            case Direction.InputOutput:
                                EmitParamLine(
                                    $"{handle} = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"{pname}\", {valueExpr}, {dbTypeExpr}{sizeArg});");
                                break;
                            case Direction.ReturnValue:
                                EmitParamLine(
                                    $"{handle} = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddReturnValueParameter(cmd, \"{pname}\", {dbTypeExpr});");
                                break;
                        }
                        if (hasProvider && direction is Direction.Output or Direction.InputOutput)
                        {
                            EmitParamLine(
                                $"(({attrs!.ProviderParameterTypeFullName}){handle}).{attrs.ProviderPropertyName} = {attrs.ProviderValueExpr};");
                        }
                    }
                    break;
                }

                case RawSqlNode r:
                    hasDynamicSql = true;
                    sb.Append("__sb.Append((")
                        .Append(r.Source)
                        .Append(")?.ToString() ?? string.Empty);\n");
                    break;

                case CodeNode c:
                    hasDynamicSql = true;
                    sb.Append(c.Code).Append('\n');
                    break;

                // UsingNode is consumed by DataAccessorGenerator at the accessor level
                // (aggregated and emitted as file-header `using` directives, spec
                // §1.4 F12 / §6.3). Skip it here to avoid duplicating in the method body.
            }
        }

        return new EmitResult(
            sb.ToString(),
            hasDynamicSql ? null : staticSql.ToString(),
            hasDynamicSql ? string.Empty : sbStaticParam.ToString(),
            undefined,
            requiresIEnumerable,
            outputBindings);
    }

    private static string ExtractRoot(string name)
    {
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
