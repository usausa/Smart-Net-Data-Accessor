namespace Smart.Data.Accessor.Generator.Sql;

using System.Text;

using Smart.Data.Accessor.Generator.Sql.Nodes;

internal static class NodeEmitter
{
    public sealed class EmitResult
    {
        public string Code { get; }

        public IReadOnlyList<string> UndefinedParameters { get; }

        public bool RequiresIEnumerable { get; }

        public EmitResult(string code, IReadOnlyList<string> undefined, bool requiresIEnumerable)
        {
            Code = code;
            UndefinedParameters = undefined;
            RequiresIEnumerable = requiresIEnumerable;
        }
    }

    /// <summary>Per-parameter attribute metadata gathered from method parameters.</summary>
    public sealed class ParameterAttributes
    {
        public string? DbTypeExpr { get; init; }   // e.g. "global::System.Data.DbType.AnsiString" or null
        public int? Size { get; init; }
    }

    public static EmitResult Emit(IReadOnlyList<INode> nodes, ISet<string> knownParameters)
        => Emit(nodes, knownParameters, _ => null);

    public static EmitResult Emit(
        IReadOnlyList<INode> nodes,
        ISet<string> knownParameters,
        Func<string, ParameterAttributes?> attrLookup)
    {
        var sb = new StringBuilder();
        var undefined = new List<string>();
        var requiresIEnumerable = false;
        var counter = 0;

        foreach (var node in nodes)
        {
            switch (node)
            {
                case SqlNode s:
                    if (!string.IsNullOrEmpty(s.Sql))
                    {
                        sb.Append("            __sb.Append(\"").Append(Escape(s.Sql)).AppendLine("\");");
                    }
                    break;

                case ParameterNode p:
                {
                    var root = ExtractRoot(p.Name);
                    if (!knownParameters.Contains(root))
                    {
                        undefined.Add(p.Name);
                    }
                    var pname = "@p" + counter++;
                    var attrs = attrLookup(root);
                    var dbTypeArg = attrs?.DbTypeExpr is { } dt ? ", " + dt : string.Empty;
                    var sizeArg = attrs?.Size is { } sz ? ", " + sz.ToString(System.Globalization.CultureInfo.InvariantCulture) : string.Empty;
                    if (p.IsMultiple)
                    {
                        requiresIEnumerable = true;
                        sb.Append("            __sb.Append(global::Smart.Data.Accessor.Engine.ExecuteEngine.AddInParameters(cmd, \"")
                            .Append(pname)
                            .Append("\", (global::System.Collections.IEnumerable)")
                            .Append(p.Name)
                            .Append(dbTypeArg)
                            .AppendLine("));");
                    }
                    else
                    {
                        sb.Append("            __sb.Append(\"").Append(pname).AppendLine("\");");
                        sb.Append("            global::Smart.Data.Accessor.Engine.ExecuteEngine.AddInParameter(cmd, \"")
                            .Append(pname)
                            .Append("\", ")
                            .Append(p.Name)
                            .Append(dbTypeArg)
                            .Append(sizeArg)
                            .AppendLine(");");
                    }
                    break;
                }

                case RawSqlNode r:
                    sb.Append("            __sb.Append((")
                        .Append(r.Source)
                        .AppendLine(")?.ToString() ?? string.Empty);");
                    break;

                case CodeNode c:
                    sb.Append("            ").AppendLine(c.Code);
                    break;

                // UsingNode is currently ignored (helpers/usings are not emitted in Phase 2 prototype).
                default:
                    break;
            }
        }

        return new EmitResult(sb.ToString(), undefined, requiresIEnumerable);
    }

    private static string ExtractRoot(string name)
    {
        var dot = name.IndexOf('.');
        return dot < 0 ? name : name[..dot];
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
