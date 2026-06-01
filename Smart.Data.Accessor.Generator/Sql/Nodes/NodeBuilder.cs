namespace Smart.Data.Accessor.Generator.Sql.Nodes;

using System.Text;

using Smart.Data.Accessor.Generator.Sql;

public sealed class NodeBuilder
{
    private readonly IReadOnlyList<Token> tokens;

    private readonly List<INode> pragmaNodes = [];

    private readonly List<INode> bodyNodes = [];

    private readonly List<string> unknownPragmas = [];

    private readonly StringBuilder sql = new();

    private int current;

    public NodeBuilder(IReadOnlyList<Token> tokens)
    {
        this.tokens = tokens;
    }

    public IReadOnlyList<string> UnknownPragmas => unknownPragmas;

    private Token? NextToken() => current + 1 < tokens.Count ? tokens[++current] : null;

    private void Flush()
    {
        if (sql.Length > 0)
        {
            bodyNodes.Add(new SqlNode(sql.ToString()));
            sql.Clear();
        }
    }

    private void AddPragmaNode(INode node)
    {
        Flush();
        pragmaNodes.Add(node);
    }

    private void AddBody(INode node)
    {
        Flush();
        bodyNodes.Add(node);
    }

    public IReadOnlyList<INode> Build()
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

        // SDA0104: unknown pragma '/*!xxx */'
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
