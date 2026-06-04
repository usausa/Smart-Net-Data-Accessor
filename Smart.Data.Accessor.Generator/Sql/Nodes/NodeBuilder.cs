namespace Smart.Data.Accessor.Generator.Sql.Nodes;

using System.Text;

using Smart.Data.Accessor.Generator.Sql;

public sealed class NodeBuilder
{
    private readonly IReadOnlyList<Token> tokens;

    private readonly List<NodeBase> pragmaNodes = [];

    private readonly List<NodeBase> bodyNodes = [];

    private readonly List<string> unknownPragmas = [];

    private readonly StringBuilder sql = new();

    private int current;

    public NodeBuilder(IReadOnlyList<Token> tokens)
    {
        this.tokens = tokens;
    }

    public IReadOnlyList<string> UnknownPragmas => unknownPragmas;

    /// <summary>Outcome of the <c>/*% %/</c> code-block brace-balance check (spec §11.6).</summary>
    public enum BraceBalance
    {
        Balanced,
        UnclosedBlock,   // SDA0506: more '{' than '}' across code blocks (a block is never closed)
        ExtraClose // SDA0507: a '}' appears with no matching '{'
    }

    /// <summary>
    /// Verifies that the C# braces across all <c>/*% %/</c> code blocks (spec §6) are balanced.
    /// The block bodies are emitted verbatim into the generated method, so an unbalanced brace would
    /// otherwise surface as a confusing C# compile error in generated code. Braces inside string /
    /// char literals and <c>//</c> line comments within a block are ignored.
    /// </summary>
    public static BraceBalance CheckBraceBalance(IReadOnlyList<NodeBase> nodes)
    {
        var depth = 0;
        foreach (var node in nodes)
        {
            if (node is not CodeNode code)
            {
                continue;
            }

            var s = code.Code;
            for (var i = 0; i < s.Length; i++)
            {
                switch (s[i])
                {
                    case '"':
                        i = SkipLiteral(s, i, '"');
                        break;
                    case '\'':
                        i = SkipLiteral(s, i, '\'');
                        break;
                    case '/' when i + 1 < s.Length && s[i + 1] == '/':
                        i = s.Length;   // line comment: ignore the rest of this block fragment
                        break;
                    case '{':
                        depth++;
                        break;
                    case '}':
                        depth--;
                        if (depth < 0)
                        {
                            return BraceBalance.ExtraClose;
                        }
                        break;
                }
            }
        }

        return depth > 0 ? BraceBalance.UnclosedBlock : BraceBalance.Balanced;
    }

    // Advances past a string ('"') or char ('\'') literal beginning at index i, honouring the '\'
    // escape. Returns the index of the closing delimiter (the caller's loop increment moves past it).
    private static int SkipLiteral(string s, int i, char delimiter)
    {
        for (var j = i + 1; j < s.Length; j++)
        {
            if (s[j] == '\\')
            {
                j++;   // skip the escaped character
                continue;
            }
            if (s[j] == delimiter)
            {
                return j;
            }
        }
        return s.Length;
    }

    private Token? NextToken() => current + 1 < tokens.Count ? tokens[++current] : null;

    private void Flush()
    {
        if (sql.Length > 0)
        {
            bodyNodes.Add(new SqlNode(sql.ToString()));
            sql.Clear();
        }
    }

    private void AddPragmaNode(NodeBase node)
    {
        Flush();
        pragmaNodes.Add(node);
    }

    private void AddBody(NodeBase node)
    {
        Flush();
        bodyNodes.Add(node);
    }

    public IReadOnlyList<NodeBase> Build()
    {
        while (current < tokens.Count)
        {
            var token = tokens[current];
            switch (token.TokenType)
            {
                case TokenType.Block:
                    sql.Append(token.Value.Trim());
                    break;
                case TokenType.OpenParenthesis:
                    sql.Append(token.Value.Trim());
                    break;
                case TokenType.Comma:
                    sql.Append(token.Value.Trim());
                    break;
                case TokenType.Blank:
                    sql.Append(' ');
                    break;
                case TokenType.CloseParenthesis:
                    sql.Append(token.Value.Trim());
                    break;
                case TokenType.Hint:
                    // Query hint flows straight into the output SQL with no whitespace side-effects.
                    sql.Append("/*+ ").Append(token.Value).Append(" */");
                    break;
                case TokenType.Comment:
                    ParseComment(token.Value.Trim());
                    break;
            }

            current++;
        }

        Flush();
        TrimBodyEdges();

        return pragmaNodes.Concat(bodyNodes).ToList();
    }

    // Trim leading/trailing whitespace at the body SQL boundary so a pragma at the
    // start or end of the source (which is hoisted to a `using` directive) doesn't
    // leave a stray space in CommandText.
    private void TrimBodyEdges()
    {
        if (bodyNodes.Count > 0 && bodyNodes[0] is SqlNode first)
        {
            var trimmed = first.Sql.TrimStart();
            if (trimmed.Length == 0)
            {
                bodyNodes.RemoveAt(0);
            }
            else if (trimmed.Length != first.Sql.Length)
            {
                bodyNodes[0] = new SqlNode(trimmed);
            }
        }

        if (bodyNodes.Count > 0 && bodyNodes[^1] is SqlNode last)
        {
            var trimmed = last.Sql.TrimEnd();
            if (trimmed.Length == 0)
            {
                bodyNodes.RemoveAt(bodyNodes.Count - 1);
            }
            else if (trimmed.Length != last.Sql.Length)
            {
                bodyNodes[^1] = new SqlNode(trimmed);
            }
        }
    }

    private void ParseComment(string value)
    {
        // Pragma
        if (value.StartsWith("!helper", StringComparison.Ordinal))
        {
            AddPragmaNode(new UsingNode(true, value[7..].Trim()));
            return;
        }

        if (value.StartsWith("!using", StringComparison.Ordinal))
        {
            AddPragmaNode(new UsingNode(false, value[6..].Trim()));
            return;
        }

        // SDA0505: unknown pragma '/*!xxx */'
        if (value.StartsWith("!", StringComparison.Ordinal))
        {
            var pragmaToken = value[1..];
            var spaceIndex = pragmaToken.IndexOf(' ');
            unknownPragmas.Add(spaceIndex >= 0 ? pragmaToken[..spaceIndex] : pragmaToken);
            return;
        }

        // Code
        if (value.StartsWith("%", StringComparison.Ordinal))
        {
            AddBody(new CodeNode(value[1..].Trim()));
        }

        // Raw
        if (value.StartsWith("#", StringComparison.Ordinal))
        {
            SkipToken();
            AddBody(new RawSqlNode(value[1..].Trim()));
        }

        // Parameter
        if (value.StartsWith("@", StringComparison.Ordinal))
        {
            var hasParenthesis = SkipToken();
            AddBody(new ParameterNode(value[1..].Trim(), hasParenthesis));
        }
    }

    private bool SkipToken()
    {
        var hasParenthesis = false;
        var token = NextToken();

        while ((token is not null) && (token.TokenType == TokenType.Blank))
        {
            token = NextToken();
        }

        if (token is not null)
        {
            if (token.TokenType == TokenType.OpenParenthesis)
            {
                hasParenthesis = true;

                var count = 1;
                while ((count > 0) && ((token = NextToken()) is not null))
                {
                    if (token.TokenType == TokenType.OpenParenthesis)
                    {
                        count++;
                    }
                    else if (token.TokenType == TokenType.CloseParenthesis)
                    {
                        count--;
                    }
                }
            }
        }

        return hasParenthesis;
    }
}
