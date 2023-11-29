namespace Smart.Data.Accessor.Nodes;

using System.Text;

using Smart.Data.Accessor.Tokenizer;

public sealed class NodeBuilder
{
    private readonly IReadOnlyList<Token> tokens;

    private readonly List<INode> pragmaNodes = [];

    private readonly List<INode> bodyNodes = [];

    private readonly StringBuilder sql = new();

    private int current;

    public NodeBuilder(IReadOnlyList<Token> tokens)
    {
        this.tokens = tokens;
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
                case TokenType.Comment:
                    ParseComment(token.Value.Trim());
                    break;
            }

            current++;
        }

        Flush();

        return pragmaNodes.Concat(bodyNodes).ToList();
    }

    private void ParseComment(string value)
    {
        // Pragma
        if (value.StartsWith("!helper", StringComparison.Ordinal))
        {
            AddPragmaNode(new UsingNode(true, value[7..].Trim()));
        }

        if (value.StartsWith("!using", StringComparison.Ordinal))
        {
            AddPragmaNode(new UsingNode(false, value[6..].Trim()));
        }

        // Code
        if (value.StartsWith('%'))
        {
            AddBody(new CodeNode(value[1..].Trim()));
        }

        // Raw
        if (value.StartsWith('#'))
        {
            SkipToken();
            AddBody(new RawSqlNode(value[1..].Trim()));
        }

        // Parameter
        if (value.StartsWith('@'))
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
