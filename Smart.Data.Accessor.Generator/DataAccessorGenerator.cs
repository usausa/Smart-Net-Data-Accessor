namespace Smart.Data.Accessor.Generator;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using Smart.Data.Accessor.Generator.Models;
using Smart.Data.Accessor.Generator.Sql;
using Smart.Data.Accessor.Generator.Sql.Nodes;

[Generator]
public sealed class DataAccessorGenerator : IIncrementalGenerator
{
    private const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";
    private const string ExecuteAttributeName = "Smart.Data.Accessor.Attributes.ExecuteAttribute";
    private const string ExecuteScalarAttributeName = "Smart.Data.Accessor.Attributes.ExecuteScalarAttribute";
    private const string QueryAttributeName = "Smart.Data.Accessor.Attributes.QueryAttribute";
    private const string QueryFirstAttributeName = "Smart.Data.Accessor.Attributes.QueryFirstAttribute";
    private const string NameAttributeName = "Smart.Data.Accessor.Attributes.NameAttribute";
    private const string IgnoreAttributeName = "Smart.Data.Accessor.Attributes.IgnoreAttribute";
    private const string DbTypeAttributeName = "Smart.Data.Accessor.Attributes.DbTypeAttribute";
    private const string SqlSizeAttributeName = "Smart.Data.Accessor.Attributes.SqlSizeAttribute";
    private const string AnsiStringAttributeName = "Smart.Data.Accessor.Attributes.AnsiStringAttribute";
    private const string CommandTimeoutAttributeName = "Smart.Data.Accessor.Attributes.CommandTimeoutAttribute";
    private const string TimeoutAttributeName = "Smart.Data.Accessor.Attributes.TimeoutAttribute";
    private const string CancellationTokenTypeName = "System.Threading.CancellationToken";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var sqlFiles = context.AdditionalTextsProvider
            .Where(static t => t.Path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .Select(static (t, ct) => (Path: System.IO.Path.GetFileNameWithoutExtension(t.Path), Text: t.GetText(ct)?.ToString() ?? string.Empty))
            .Collect();

        var classes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                DataAccessorAttributeName,
                static (s, _) => s is ClassDeclarationSyntax,
                static (ctx, _) => ctx)
            .Collect();

        context.RegisterSourceOutput(
            classes.Combine(sqlFiles),
            static (spc, source) => Execute(spc, source.Left, source.Right));
    }

    private static void Execute(
        SourceProductionContext context,
        ImmutableArray<GeneratorAttributeSyntaxContext> classes,
        ImmutableArray<(string Path, string Text)> sqlFiles)
    {
        var sqlMap = sqlFiles.ToDictionary(static x => x.Path, static x => x.Text, StringComparer.Ordinal);

        foreach (var ctx in classes)
        {
            var syntax = (ClassDeclarationSyntax)ctx.TargetNode;
            if (ctx.TargetSymbol is not INamedTypeSymbol classSymbol)
            {
                continue;
            }

            if (!syntax.Modifiers.Any(m => m.Text == "partial"))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.InvalidClass, syntax.Identifier.GetLocation(), classSymbol.Name));
                continue;
            }

            var model = BuildAccessorModel(context, classSymbol, sqlMap);
            if (model is null)
            {
                continue;
            }

            var source = Emit(model);
            var ns = string.IsNullOrEmpty(model.Namespace) ? "global" : model.Namespace!.Replace('.', '_');
            var filename = $"{ns}_{model.ClassName}.g.cs";
            context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
        }
    }

    private static AccessorModel? BuildAccessorModel(
        SourceProductionContext context,
        INamedTypeSymbol classSymbol,
        Dictionary<string, string> sqlMap)
    {
        var methods = new List<MethodModel>();
        foreach (var member in classSymbol.GetMembers().OfType<IMethodSymbol>())
        {
            if (member.MethodKind != MethodKind.Ordinary || !member.IsPartialDefinition)
            {
                continue;
            }

            string? kind = null;
            string? builder = null;
            foreach (var attr in member.GetAttributes())
            {
                var fullName = attr.AttributeClass?.ToDisplayString();
                if (fullName == ExecuteAttributeName || fullName == ExecuteScalarAttributeName)
                {
                    kind = "Execute";
                    builder = ReadBuilderProperty(attr);
                }
                else if (fullName == QueryAttributeName || fullName == QueryFirstAttributeName)
                {
                    kind = "Query";
                    builder = ReadBuilderProperty(attr);
                }
            }

            if (kind is null)
            {
                continue;
            }

            var sqlKey = $"{classSymbol.Name}.{member.Name}";
            sqlMap.TryGetValue(sqlKey, out var sql);

            if (sql is null && builder is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SqlNotFound, member.Locations.FirstOrDefault(), member.Name, sqlKey + ".sql"));
                continue;
            }

            var parameters = member.Parameters.Select(p =>
            {
                string? dbTypeExpr = null;
                int? size = null;
                foreach (var pa in p.GetAttributes())
                {
                    var an = pa.AttributeClass?.ToDisplayString();
                    if (an == DbTypeAttributeName && pa.ConstructorArguments.Length > 0 && pa.ConstructorArguments[0].Value is int dt)
                    {
                        dbTypeExpr = $"(global::System.Data.DbType){dt}";
                    }
                    else if (an == AnsiStringAttributeName)
                    {
                        dbTypeExpr ??= "global::System.Data.DbType.AnsiString";
                    }
                    else if (an == SqlSizeAttributeName && pa.ConstructorArguments.Length > 0 && pa.ConstructorArguments[0].Value is int sz2)
                    {
                        size = sz2;
                    }
                }
                return new ParameterModel(
                    p.Name,
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.NullableAnnotation == NullableAnnotation.Annotated,
                    p.Type.ToDisplayString() == CancellationTokenTypeName,
                    dbTypeExpr,
                    size);
            }).ToList();

            // Method-level [CommandTimeout(N)] / [Timeout(N)]
            int? commandTimeout = null;
            foreach (var ma in member.GetAttributes())
            {
                var an = ma.AttributeClass?.ToDisplayString();
                if ((an == CommandTimeoutAttributeName || an == TimeoutAttributeName) &&
                    ma.ConstructorArguments.Length > 0 &&
                    ma.ConstructorArguments[0].Value is int sec)
                {
                    commandTimeout = sec;
                }
            }

            var shape = ClassifyReturn(member.ReturnType, out var scalarFq, out var elementFq, out var entitySymbol);
            if (shape is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedReturn, member.Locations.FirstOrDefault(), member.Name, member.ReturnType.ToDisplayString()));
                continue;
            }

            // For Query kind, list/asyncenum must have a mappable element type.
            IReadOnlyList<string>? columnAssignments = null;
            if (kind == "Query")
            {
                var mapTarget = elementFq is not null ? entitySymbol : null;
                if (mapTarget is null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedReturn, member.Locations.FirstOrDefault(), member.Name, member.ReturnType.ToDisplayString()));
                    continue;
                }
                columnAssignments = BuildColumnAssignments(mapTarget);
            }

            // Tokenize & emit SQL when a literal SQL is provided (no Builder).
            string? sqlEmitCode = null;
            if (sql is not null && builder is null)
            {
                sqlEmitCode = BuildSqlEmitCode(context, member, parameters, sql);
            }

            methods.Add(new MethodModel(
                member.Name,
                kind,
                member.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                shape.Value,
                scalarFq,
                elementFq,
                AccessibilityText(member.DeclaredAccessibility),
                parameters,
                builder,
                sql,
                sqlEmitCode,
                columnAssignments,
                commandTimeout));
        }

        if (methods.Count == 0)
        {
            return null;
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        // Read [DataAccessor(Dialect = typeof(XxxDialect))].
        string? dialectFq = null;
        foreach (var attr in classSymbol.GetAttributes())
        {
            if (attr.AttributeClass?.ToDisplayString() != DataAccessorAttributeName)
            {
                continue;
            }
            foreach (var kv in attr.NamedArguments)
            {
                if (kv.Key == "Dialect" && kv.Value.Value is INamedTypeSymbol t)
                {
                    dialectFq = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }
        }

        return new AccessorModel(
            ns,
            classSymbol.Name,
            AccessibilityText(classSymbol.DeclaredAccessibility),
            "global::System.Data.Common.DbConnection",
            dialectFq,
            methods);
    }

    private static ReturnShape? ClassifyReturn(
        ITypeSymbol returnType,
        out string? scalarFq,
        out string? elementFq,
        out INamedTypeSymbol? elementSymbol)
    {
        scalarFq = null;
        elementFq = null;
        elementSymbol = null;

        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return ReturnShape.Void;
        }

        if (returnType is INamedTypeSymbol named)
        {
            var fq = named.ConstructedFrom.ToDisplayString();

            // Task / ValueTask (non-generic)
            if (fq == "System.Threading.Tasks.Task")
            {
                return ReturnShape.Task;
            }
            if (fq == "System.Threading.Tasks.ValueTask")
            {
                return ReturnShape.ValueTask;
            }

            if (named.IsGenericType)
            {
                var arg = named.TypeArguments[0];

                if (fq is "System.Threading.Tasks.Task<TResult>" or "System.Threading.Tasks.Task<T>")
                {
                    if (IsListLike(arg, out elementFq, out elementSymbol))
                    {
                        return ReturnShape.TaskList;
                    }
                    scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    elementFq = scalarFq;
                    return ReturnShape.TaskScalar;
                }
                if (fq is "System.Threading.Tasks.ValueTask<TResult>" or "System.Threading.Tasks.ValueTask<T>")
                {
                    if (IsListLike(arg, out elementFq, out elementSymbol))
                    {
                        return ReturnShape.TaskList;
                    }
                    scalarFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    elementFq = scalarFq;
                    return ReturnShape.ValueTaskScalar;
                }
                if (fq == "System.Collections.Generic.IAsyncEnumerable<T>")
                {
                    elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    elementSymbol = arg as INamedTypeSymbol;
                    return ReturnShape.AsyncEnumerable;
                }
                if (IsListLike(returnType, out elementFq, out elementSymbol))
                {
                    return ReturnShape.List;
                }
            }
        }

        // Plain scalar (int, string, etc.)
        scalarFq = returnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return ReturnShape.Scalar;
    }

    private static bool IsListLike(ITypeSymbol type, out string? elementFq, out INamedTypeSymbol? elementSymbol)
    {
        elementFq = null;
        elementSymbol = null;
        if (type is not INamedTypeSymbol named || !named.IsGenericType)
        {
            return false;
        }
        var fq = named.ConstructedFrom.ToDisplayString();
        if (fq is "System.Collections.Generic.List<T>"
            or "System.Collections.Generic.IList<T>"
            or "System.Collections.Generic.IReadOnlyList<T>"
            or "System.Collections.Generic.IEnumerable<T>"
            or "System.Collections.Generic.IReadOnlyCollection<T>"
            or "System.Collections.Generic.ICollection<T>")
        {
            var arg = named.TypeArguments[0];
            elementFq = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            elementSymbol = arg as INamedTypeSymbol;
            return elementSymbol is not null;
        }
        return false;
    }

    private static List<string> BuildColumnAssignments(INamedTypeSymbol entity)
    {
        var assignments = new List<string>();
        foreach (var prop in entity.GetMembers().OfType<IPropertySymbol>())
        {
            if (prop.DeclaredAccessibility != Accessibility.Public || prop.IsStatic || prop.SetMethod is null)
            {
                continue;
            }
            // [Ignore] now means exclude everywhere (phase 2 §2.3).
            if (prop.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == IgnoreAttributeName))
            {
                continue;
            }
            var name = prop.Name;
            var column = prop.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NameAttributeName)
                ?.ConstructorArguments.FirstOrDefault().Value as string ?? name;
            var typeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            assignments.Add($"            {name} = global::Smart.Data.Accessor.Engine.ExecuteEngine.GetValue<{typeName}>(reader, reader.GetOrdinal(\"{column}\")),");
        }
        return assignments;
    }

    private static string? ReadBuilderProperty(AttributeData attr)
    {
        foreach (var kv in attr.NamedArguments)
        {
            if (kv.Key == "Builder" && kv.Value.Value is string s)
            {
                return s;
            }
        }
        return null;
    }

    private static string AccessibilityText(Accessibility a) => a switch
    {
        Accessibility.Public => "public",
        Accessibility.Internal => "internal",
        Accessibility.Private => "private",
        Accessibility.Protected => "protected",
        Accessibility.ProtectedOrInternal => "protected internal",
        Accessibility.ProtectedAndInternal => "private protected",
        _ => "internal",
    };

    //--------------------------------------------------------------------------------
    // Emit
    //--------------------------------------------------------------------------------

    private static string Emit(AccessorModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("#pragma warning disable");
        sb.AppendLine();
        if (!string.IsNullOrEmpty(model.Namespace))
        {
            sb.AppendLine($"namespace {model.Namespace};");
            sb.AppendLine();
        }
        sb.AppendLine($"{model.Accessibility} partial class {model.ClassName}");
        sb.AppendLine("{");
        sb.AppendLine($"    private readonly {model.ConnectionFieldType} connection;");
        sb.AppendLine();
        sb.AppendLine($"    public {model.ClassName}({model.ConnectionFieldType} connection)");
        sb.AppendLine("    {");
        sb.AppendLine("        this.connection = connection;");
        sb.AppendLine("    }");

        foreach (var m in model.Methods)
        {
            sb.AppendLine();
            EmitMethod(sb, m, model.DialectTypeFullName);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static bool IsAsyncShape(ReturnShape s) =>
        s is ReturnShape.Task or ReturnShape.TaskScalar or ReturnShape.TaskList
          or ReturnShape.ValueTask or ReturnShape.ValueTaskScalar or ReturnShape.AsyncEnumerable;

    private static void EmitMethod(StringBuilder sb, MethodModel m, string? dialectFq)
    {
        var paramList = string.Join(", ", m.Parameters.Select(p => $"{p.TypeFullName} {p.Name}"));
        var asyncKw = IsAsyncShape(m.ReturnShape) ? "async " : string.Empty;
        sb.AppendLine($"    {m.Accessibility} {asyncKw}partial {m.ReturnTypeFullName} {m.Name}({paramList})");
        sb.AppendLine("    {");

        // Cancellation token discovery
        var ct = m.Parameters.FirstOrDefault(p => p.IsCancellationToken);
        var ctExpr = ct?.Name ?? "default";

        sb.AppendLine("        using var cmd = this.connection.CreateCommand();");
        if (m.CommandTimeoutSeconds is { } cts)
        {
            sb.AppendLine($"        cmd.CommandTimeout = {cts};");
        }

        var dialectArg = dialectFq is null ? string.Empty : $", {dialectFq}.Instance";

        // SQL / parameter setup
        if (m.BuilderMethodName is not null)
        {
            sb.AppendLine("        var __sb = global::Smart.Data.Accessor.Engine.StringBuilderPool.Rent();");
            sb.AppendLine($"        var ctx = new global::Smart.Data.Accessor.Builders.BuilderContext(__sb, cmd{dialectArg});");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            var nonCtArgs = m.Parameters.Where(p => !p.IsCancellationToken).Select(p => p.Name);
            var args = string.Join(", ", new[] { "ref ctx" }.Concat(nonCtArgs));
            // Note: BuilderContext is ref struct; pass by ref.
            sb.AppendLine($"            {m.BuilderMethodName}({args});");
            sb.AppendLine("            cmd.CommandText = ctx.ToCommandText();");
            sb.AppendLine("        }");
            sb.AppendLine("        finally");
            sb.AppendLine("        {");
            sb.AppendLine("            ctx.Dispose();");
            sb.AppendLine("        }");
        }
        else
        {
            // Tokenized 2-way SQL → emit StringBuilder build code.
            sb.AppendLine("        var __sb = global::Smart.Data.Accessor.Engine.StringBuilderPool.Rent();");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            if (!string.IsNullOrEmpty(m.SqlEmitCode))
            {
                sb.Append(m.SqlEmitCode);
            }
            sb.AppendLine("            cmd.CommandText = __sb.ToString();");
            sb.AppendLine("        }");
            sb.AppendLine("        finally");
            sb.AppendLine("        {");
            sb.AppendLine("            global::Smart.Data.Accessor.Engine.StringBuilderPool.Return(__sb);");
            sb.AppendLine("        }");
        }

        EmitInvocation(sb, m, ctExpr);

        sb.AppendLine("    }");
    }

    private static void EmitInvocation(StringBuilder sb, MethodModel m, string ctExpr)
    {
        if (m.MethodKind == "Execute")
        {
            switch (m.ReturnShape)
            {
                case ReturnShape.Void:
                    sb.AppendLine("        global::Smart.Data.Accessor.Engine.ExecuteEngine.Execute(cmd);");
                    break;
                case ReturnShape.Scalar:
                    // int Execute / scalar
                    if (m.ScalarTypeFullName == "int")
                    {
                        sb.AppendLine("        return global::Smart.Data.Accessor.Engine.ExecuteEngine.Execute(cmd);");
                    }
                    else
                    {
                        sb.AppendLine($"        return global::Smart.Data.Accessor.Engine.ExecuteEngine.ExecuteScalar<{m.ScalarTypeFullName}>(cmd)!;");
                    }
                    break;
                case ReturnShape.Task:
                    sb.AppendLine($"        await global::Smart.Data.Accessor.Engine.ExecuteEngine.ExecuteAsync(cmd, {ctExpr}).ConfigureAwait(false);");
                    break;
                case ReturnShape.TaskScalar:
                case ReturnShape.ValueTaskScalar:
                    if (m.ScalarTypeFullName == "int")
                    {
                        sb.AppendLine($"        return await global::Smart.Data.Accessor.Engine.ExecuteEngine.ExecuteAsync(cmd, {ctExpr}).ConfigureAwait(false);");
                    }
                    else
                    {
                        sb.AppendLine($"        return (await global::Smart.Data.Accessor.Engine.ExecuteEngine.ExecuteScalarAsync<{m.ScalarTypeFullName}>(cmd, {ctExpr}).ConfigureAwait(false))!;");
                    }
                    break;
                case ReturnShape.ValueTask:
                    sb.AppendLine($"        await global::Smart.Data.Accessor.Engine.ExecuteEngine.ExecuteAsync(cmd, {ctExpr}).ConfigureAwait(false);");
                    break;
                default:
                    sb.AppendLine("        // unsupported Execute shape");
                    break;
            }
            return;
        }

        // Query
        var mapLambda = BuildMapLambda(m);
        switch (m.ReturnShape)
        {
            case ReturnShape.List:
                sb.AppendLine($"        return global::Smart.Data.Accessor.Engine.ExecuteEngine.QueryBuffer<{m.ElementTypeFullName}>(cmd, {mapLambda});");
                break;
            case ReturnShape.TaskList:
                sb.AppendLine($"        return await global::Smart.Data.Accessor.Engine.ExecuteEngine.QueryBufferAsync<{m.ElementTypeFullName}>(cmd, {mapLambda}, {ctExpr}).ConfigureAwait(false);");
                break;
            case ReturnShape.AsyncEnumerable:
                // Forward to engine iterator; user must add [EnumeratorCancellation] on their CT param.
                sb.AppendLine($"        await foreach (var __item in global::Smart.Data.Accessor.Engine.ExecuteEngine.QueryAsync<{m.ElementTypeFullName}>(cmd, {mapLambda}, {ctExpr}).ConfigureAwait(false))");
                sb.AppendLine("        {");
                sb.AppendLine("            yield return __item;");
                sb.AppendLine("        }");
                break;
            case ReturnShape.Scalar:
                // QueryFirst-style: return single mapped item
                sb.AppendLine($"        return global::Smart.Data.Accessor.Engine.ExecuteEngine.QueryFirstOrDefault<{m.ElementTypeFullName}>(cmd, {mapLambda})!;");
                break;
            case ReturnShape.TaskScalar:
            case ReturnShape.ValueTaskScalar:
                sb.AppendLine($"        return (await global::Smart.Data.Accessor.Engine.ExecuteEngine.QueryFirstOrDefaultAsync<{m.ElementTypeFullName}>(cmd, {mapLambda}, {ctExpr}).ConfigureAwait(false))!;");
                break;
            default:
                sb.AppendLine("        // unsupported Query shape");
                break;
        }
    }

    private static string BuildMapLambda(MethodModel m)
    {
        var sb = new StringBuilder();
        sb.Append($"static reader => new {m.ElementTypeFullName} {{ ");
        if (m.QueryColumnAssignments is not null)
        {
            // strip leading indentation / commas-newlines; reuse the same expressions.
            var first = true;
            foreach (var raw in m.QueryColumnAssignments)
            {
                var trimmed = raw.TrimStart();
                if (trimmed.EndsWith(",", StringComparison.Ordinal))
                {
                    trimmed = trimmed[..^1];
                }
                if (!first)
                {
                    sb.Append(", ");
                }
                first = false;
                sb.Append(trimmed);
            }
        }
        sb.Append(" }");
        return sb.ToString();
    }

    //--------------------------------------------------------------------------------
    // 2-way SQL tokenization + emit (Phase 2 §3.1)
    //--------------------------------------------------------------------------------

    private static string BuildSqlEmitCode(
        SourceProductionContext context,
        IMethodSymbol member,
        IReadOnlyList<ParameterModel> parameters,
        string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SqlEmpty, member.Locations.FirstOrDefault(), member.Name));
            return string.Empty;
        }

        IReadOnlyList<INode> nodes;
        try
        {
            var tokenizer = new SqlTokenizer(sql);
            var tokens = tokenizer.Tokenize();
            var normalized = SqlTokenNormalizer.Normalize(tokens);
            var builder = new NodeBuilder(normalized);
            nodes = builder.Build();
        }
        catch (SqlTokenizerException ex)
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SqlTokenizeFailed, member.Locations.FirstOrDefault(), member.Name, ex.Message));
            return string.Empty;
        }

        var known = new HashSet<string>(parameters.Where(p => !p.IsCancellationToken).Select(p => p.Name), StringComparer.Ordinal);
        var paramMap = parameters.ToDictionary(p => p.Name, p => p, StringComparer.Ordinal);
        var result = NodeEmitter.Emit(nodes, known, name =>
        {
            if (paramMap.TryGetValue(name, out var pm) && (pm.DbTypeExpr is not null || pm.Size is not null))
            {
                return new NodeEmitter.ParameterAttributes { DbTypeExpr = pm.DbTypeExpr, Size = pm.Size };
            }
            return null;
        });

        foreach (var u in result.UndefinedParameters.Distinct(StringComparer.Ordinal))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UndefinedSqlParameter, member.Locations.FirstOrDefault(), member.Name, u));
        }

        // SDA0111: method parameter not referenced in SQL (Info only).
        var referenced = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in nodes)
        {
            switch (node)
            {
                case ParameterNode pn:
                    referenced.Add(ExtractRoot(pn.Name));
                    break;
                case RawSqlNode rn:
                    referenced.Add(ExtractRoot(rn.Source));
                    break;
                case CodeNode cn:
                    // Best-effort: any whole-word identifier matching a parameter name counts.
                    foreach (var p in parameters)
                    {
                        if (cn.Code.IndexOf(p.Name, StringComparison.Ordinal) >= 0)
                        {
                            referenced.Add(p.Name);
                        }
                    }
                    break;
            }
        }
        foreach (var p in parameters)
        {
            if (p.IsCancellationToken)
            {
                continue;
            }
            if (!referenced.Contains(p.Name))
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnusedMethodParameter, member.Locations.FirstOrDefault(), member.Name, p.Name));
            }
        }

        return result.Code;
    }

    private static string ExtractRoot(string name)
    {
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }
}
