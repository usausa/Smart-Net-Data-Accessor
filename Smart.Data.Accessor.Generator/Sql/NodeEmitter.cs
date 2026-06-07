namespace Smart.Data.Accessor.Generator.Sql;

using System.Globalization;
using System.Text;

using Smart.Data.Accessor.Generator.Helpers;
using Smart.Data.Accessor.Generator.Sql.Nodes;
using Smart.Data.Accessor.Shared.Helpers;

internal static class NodeEmitter
{
    public sealed class EmitResult
    {
        // SQL build code lines (StringBuilderPool path), one statement per line, with no leading
        // indentation. The caller indents each line via SourceBuilder at its current IndentLevel.
        public string Code { get; }

        // Non-null when the SQL has no dynamic branches (no CodeNode / RawSqlNode and no multi-value
        // /*@ list */ parameter). In that case the literal SQL text is pre-built at code-gen time and
        // emitted as `cmd.CommandText = "..."`, bypassing StringBuilderPool.Rent / Return at runtime.
        public string? StaticSqlText { get; }

        // Parameter setup code lines used when StaticSqlText is non-null. One statement per line,
        // with no leading indentation; caller controls indent.
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

    // Output / InOut / ReturnValue binding captured during emission. The Generator pre-declares the
    // handle variable before the SQL build try-block and uses it after Execute to write the parameter
    // value back to out/ref args.
    public sealed record OutputBinding(string ParameterName, string HandleName);

    // Per-parameter attribute metadata gathered from method parameters.
    public sealed class ParameterAttributes
    {
        public string? DbTypeExpression { get; init; }   // e.g. "global::System.Data.DbType.AnsiString" or null
        public int? Size { get; init; }
        public Direction Direction { get; init; } = Direction.Input;
        public string? OutputHandleName { get; init; }   // optional local variable name to capture DbParameter handle

        // FQN of the enum's underlying primitive when this parameter is an enum (or Nullable<enum>);
        // used to emit an explicit cast so AssignValue's runtime Convert.ChangeType is bypassed. Only
        // applied when the SQL marker references the bare parameter name (no member access path).
        public string? EnumUnderlyingFullName { get; init; }

        public bool IsNullableEnum { get; init; }

        // provider-specific DbType assignment emitted after the
        // AddInParameter/AddOutParameter/AddInOutParameter call.
        public string? ProviderParameterTypeFullName { get; init; }
        public string? ProviderPropertyName { get; init; }
        public string? ProviderValueExpression { get; init; }

        // non-null when a [TypeHandler<>] applies to this parameter; the bound value is written via
        // TConverter.ToDb(...). Applied only to a bare parameter marker (no member-access path).
        // ConverterValueIsNullable adds a HasValue guard for Nullable<TClr>.
        public string? ConverterTypeFullName { get; init; }
        public bool ConverterValueIsNullable { get; init; }

        // the converter's IValueConverter<TDb, TClr> type-argument FQNs, for emitting
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
        Func<string, ParameterAttributes?> attributeLookup)
        => Emit(nodes, knownParameters, attributeLookup, '@');

    public static EmitResult Emit(
        IReadOnlyList<NodeBase> nodes,
        ISet<string> knownParameters,
        Func<string, ParameterAttributes?> attributeLookup,
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
                case SqlNode sqlNode:
                    if (!String.IsNullOrEmpty(sqlNode.Sql))
                    {
                        sb.Append("__sb.Append(\"").Append(Escape(sqlNode.Sql)).Append("\");\n");
                        staticSql.Append(sqlNode.Sql);
                    }
                    break;

                case ParameterNode parameterNode:
                {
                    var root = StringHelper.ExtractRoot(parameterNode.Name);
                    if (!knownParameters.Contains(root))
                    {
                        undefined.Add(parameterNode.Name);
                    }
                    var parameterName = bindMarker + "p" + counter++;
                    var attributes = attributeLookup(root);
                    var sizeArg = attributes?.Size is { } size ? ", " + size.ToString(CultureInfo.InvariantCulture) : string.Empty;
                    var direction = attributes?.Direction ?? Direction.Input;
                    // Apply the converter (ToDb) or the enum-cast only when the SQL token is the bare
                    // parameter (no member access) — for `entity.X` the leaf type is unknown to this
                    // layer. A converter takes priority over the enum cast.
                    var valueExpression = parameterNode.Name;
                    var inMethod = "AddInParameter";
                    if (!parameterNode.Name.Contains('.'))
                    {
                        if (attributes?.ConverterTypeFullName is { } converter)
                        {
                            // Bind via the converter-sharing overload; valueExpression stays raw (the helper
                            // calls TConverter.ToDb + handles null/Nullable).
                            inMethod = CodeExpressionHelper.AddInParameterConverter(converter, attributes.ConverterDbTypeFullName!, attributes.ConverterClrTypeFullName!);
                        }
                        else if (attributes?.EnumUnderlyingFullName is { } enumUnderlying)
                        {
                            valueExpression = CodeExpressionHelper.EnumCastValue(enumUnderlying, attributes.IsNullableEnum, parameterNode.Name);
                        }
                    }
                    var hasProvider = (attributes?.ProviderParameterTypeFullName is not null)
                        && (attributes.ProviderPropertyName is not null)
                        && (attributes.ProviderValueExpression is not null);
                    if (parameterNode.IsMultiple)
                    {
                        // /*@ list */ expands to a comma-separated placeholder list whose
                        // arity depends on the IEnumerable count -> SQL text is dynamic.
                        hasDynamicSql = true;
                        requiresIEnumerable = true;
                        sb.Append("__sb.Append(global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInParameters(cmd, \"")
                            .Append(parameterName)
                            .Append("\", ")
                            .Append(parameterNode.Name)
                            .Append(CodeExpressionHelper.DbTypeSizeArgs(attributes?.DbTypeExpression, null))
                            .Append("));\n");
                    }
                    else if (direction == Direction.Input)
                    {
                        sb.Append("__sb.Append(\"").Append(parameterName).Append("\");\n");
                        staticSql.Append(parameterName);
                        if (hasProvider)
                        {
                            // Skip the positional DbType? slot — provider-specific assignment
                            // will set the native typed property. Use named arg for size to
                            // keep the call unambiguous.
                            var providerSizeArg = attributes?.Size is { } providerSize
                                ? ", size: " + providerSize.ToString(CultureInfo.InvariantCulture)
                                : string.Empty;
                            EmitParamLine(
                                $"(({attributes!.ProviderParameterTypeFullName})global::Smart.Data.Accessor.Helpers.ExecuteHelper.{inMethod}(cmd, \"{parameterName}\", {valueExpression}{providerSizeArg})).{attributes.ProviderPropertyName} = {attributes.ProviderValueExpression};");
                        }
                        else
                        {
                            EmitParamLine(
                                $"global::Smart.Data.Accessor.Helpers.ExecuteHelper.{inMethod}(cmd, \"{parameterName}\", {valueExpression}{CodeExpressionHelper.DbTypeSizeArgs(attributes?.DbTypeExpression, attributes?.Size)});");
                        }
                    }
                    else
                    {
                        // OUT / InOut / ReturnValue — DbType is required.
                        var dbTypeExpression = attributes?.DbTypeExpression ?? "global::System.Data.DbType.Object";
                        var handle = attributes?.OutputHandleName ?? $"__op_{parameterNode.Name}";
                        outputBindings.Add(new OutputBinding(parameterNode.Name, handle));
                        sb.Append("__sb.Append(\"").Append(parameterName).Append("\");\n");
                        staticSql.Append(parameterName);
                        switch (direction)
                        {
                            case Direction.Output:
                                EmitParamLine(
                                    $"{handle} = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddOutParameter(cmd, \"{parameterName}\", {dbTypeExpression}{sizeArg});");
                                break;
                            case Direction.InputOutput:
                                EmitParamLine(
                                    $"{handle} = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddInOutParameter(cmd, \"{parameterName}\", {valueExpression}, {dbTypeExpression}{sizeArg});");
                                break;
                            case Direction.ReturnValue:
                                EmitParamLine(
                                    $"{handle} = global::Smart.Data.Accessor.Helpers.ExecuteHelper.AddReturnValueParameter(cmd, \"{parameterName}\", {dbTypeExpression});");
                                break;
                        }
                        if (hasProvider && (direction is Direction.Output or Direction.InputOutput))
                        {
                            EmitParamLine(
                                $"(({attributes!.ProviderParameterTypeFullName}){handle}).{attributes.ProviderPropertyName} = {attributes.ProviderValueExpression};");
                        }
                    }
                    break;
                }

                case RawSqlNode rawSqlNode:
                    hasDynamicSql = true;
                    sb.Append("__sb.Append((")
                        .Append(rawSqlNode.Source)
                        .Append(")?.ToString() ?? string.Empty);\n");
                    break;

                case CodeNode codeNode:
                    hasDynamicSql = true;
                    sb.Append(codeNode.Code).Append('\n');
                    break;

                // UsingNode is consumed by DataAccessorGenerator at the accessor level (aggregated and
                // emitted as file-header `using` directives). Skip it here to avoid duplicating in the
                // method body.
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

    private static string Escape(string text) => text.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
