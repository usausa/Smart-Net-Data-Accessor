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

[Generator]
public sealed class DataAccessorGenerator : IIncrementalGenerator
{
    private const string DataAccessorAttributeName = "Smart.Data.Accessor.Attributes.DataAccessorAttribute";
    private const string ExecuteAttributeName = "Smart.Data.Accessor.Attributes.ExecuteAttribute";
    private const string QueryAttributeName = "Smart.Data.Accessor.Attributes.QueryAttribute";
    private const string NameAttributeName = "Smart.Data.Accessor.Attributes.NameAttribute";

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
                if (fullName == ExecuteAttributeName)
                {
                    kind = "Execute";
                    builder = ReadBuilderProperty(attr);
                }
                else if (fullName == QueryAttributeName)
                {
                    kind = "Query";
                    builder = ReadBuilderProperty(attr);
                }
            }

            if (kind is null)
            {
                continue;
            }

            // Resolve embedded SQL from additional files
            var sqlKey = $"{classSymbol.Name}.{member.Name}";
            sqlMap.TryGetValue(sqlKey, out var sql);

            if (sql is null && builder is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Diagnostics.SqlNotFound, member.Locations.FirstOrDefault(), member.Name, sqlKey + ".sql"));
                continue;
            }

            var parameters = member.Parameters.Select(p => new ParameterModel(
                p.Name,
                p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                p.NullableAnnotation == NullableAnnotation.Annotated)).ToList();

            string? elementType = null;
            IReadOnlyList<string>? columnAssignments = null;
            if (kind == "Query")
            {
                if (!TryResolveQueryReturnType(member.ReturnType, out elementType, out var entitySymbol))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Diagnostics.UnsupportedReturn, member.Locations.FirstOrDefault(), member.Name, member.ReturnType.ToDisplayString()));
                    continue;
                }
                columnAssignments = BuildColumnAssignments(entitySymbol!);
            }

            methods.Add(new MethodModel(
                member.Name,
                kind,
                member.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                member.ReturnsVoid,
                elementType,
                AccessibilityText(member.DeclaredAccessibility),
                parameters,
                builder,
                sql,
                columnAssignments));
        }

        if (methods.Count == 0)
        {
            return null;
        }

        var ns = classSymbol.ContainingNamespace.IsGlobalNamespace
            ? string.Empty
            : classSymbol.ContainingNamespace.ToDisplayString();

        return new AccessorModel(
            ns,
            classSymbol.Name,
            AccessibilityText(classSymbol.DeclaredAccessibility),
            "global::System.Data.Common.DbConnection",
            methods);
    }

    private static bool TryResolveQueryReturnType(ITypeSymbol returnType, out string? elementType, out INamedTypeSymbol? entitySymbol)
    {
        elementType = null;
        entitySymbol = null;
        if (returnType is INamedTypeSymbol named && named.IsGenericType &&
            (named.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.List<T>" ||
             named.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IList<T>" ||
             named.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IReadOnlyList<T>"))
        {
            var arg = named.TypeArguments[0];
            elementType = arg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            entitySymbol = arg as INamedTypeSymbol;
            return entitySymbol is not null;
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
            var name = prop.Name;
            var column = prop.GetAttributes()
                .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NameAttributeName)
                ?.ConstructorArguments.FirstOrDefault().Value as string ?? name;
            var typeName = prop.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            assignments.Add($"            {name} = global::Smart.Data.Accessor.Engine.SimpleExecuteEngine.GetValue<{typeName}>(reader, reader.GetOrdinal(\"{column}\")),");
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
            EmitMethod(sb, m);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitMethod(StringBuilder sb, MethodModel m)
    {
        var paramList = string.Join(", ", m.Parameters.Select(p => $"{p.TypeFullName} {p.Name}"));
        sb.AppendLine($"    {m.Accessibility} partial {m.ReturnTypeFullName} {m.Name}({paramList})");
        sb.AppendLine("    {");
        sb.AppendLine("        using var cmd = this.connection.CreateCommand();");

        if (m.BuilderMethodName is not null)
        {
            sb.AppendLine("        var sql = new global::System.Text.StringBuilder();");
            sb.AppendLine("        var ctx = new global::Smart.Data.Accessor.Builders.BuilderContext(sql, cmd);");
            var args = string.Join(", ", new[] { "ctx" }.Concat(m.Parameters.Select(p => p.Name)));
            sb.AppendLine($"        {m.BuilderMethodName}({args});");
            sb.AppendLine("        cmd.CommandText = sql.ToString();");
        }
        else
        {
            var literal = EscapeForVerbatim(m.EmbeddedSql ?? string.Empty);
            sb.AppendLine($"        cmd.CommandText = @\"{literal}\";");
            foreach (var p in m.Parameters)
            {
                sb.AppendLine($"        global::Smart.Data.Accessor.Engine.SimpleExecuteEngine.AddInParameter(cmd, \"@{p.Name}\", {p.Name});");
            }
        }

        if (m.MethodKind == "Execute")
        {
            if (m.ReturnsVoid)
            {
                sb.AppendLine("        global::Smart.Data.Accessor.Engine.SimpleExecuteEngine.Execute(cmd);");
            }
            else
            {
                sb.AppendLine("        return global::Smart.Data.Accessor.Engine.SimpleExecuteEngine.Execute(cmd);");
            }
        }
        else if (m.MethodKind == "Query")
        {
            sb.AppendLine($"        return global::Smart.Data.Accessor.Engine.SimpleExecuteEngine.QueryBuffer<{m.ElementTypeFullName}>(cmd, static reader => new {m.ElementTypeFullName}");
            sb.AppendLine("        {");
            if (m.QueryColumnAssignments is not null)
            {
                foreach (var a in m.QueryColumnAssignments)
                {
                    sb.AppendLine(a);
                }
            }
            sb.AppendLine("        });");
        }

        sb.AppendLine("    }");
    }

    private static string EscapeForVerbatim(string s) => s.Replace("\"", "\"\"");
}
