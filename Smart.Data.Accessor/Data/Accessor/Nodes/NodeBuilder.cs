namespace Smart.Data.Accessor.Nodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Smart.Data.Accessor.Tokenizer;

    public sealed class NodeBuilder
    {
        private readonly IReadOnlyList<Token> tokens;

        private readonly List<INode> pragmaNodes = new();

        private readonly List<INode> bodyNodes = new();

        private readonly StringBuilder sql = new();

        private int current;

        private bool lastParenthesis;

        public NodeBuilder(IReadOnlyList<Token> tokens)
        {
            this.tokens = tokens;
        }

        private Token NextToken() => current + 1 < tokens.Count ? tokens[++current] : null;

        private void Flush(bool appendBlank)
        {
            if (sql.Length > 0)
            {
                if (appendBlank)
                {
                    sql.Append(' ');
                }

                bodyNodes.Add(new SqlNode(sql.ToString()));
                sql.Clear();
            }
        }

        private void AddPragmaNode(INode node)
        {
            Flush(true);
            pragmaNodes.Add(node);
        }

        private void AddBody(INode node, bool appendBlank)
        {
            Flush(appendBlank);
            bodyNodes.Add(node);
            lastParenthesis = false;
        }

        private void AppendSql(string value, bool appendBlank)
        {
            if (!lastParenthesis && appendBlank && ((sql.Length > 0) || (bodyNodes.Count > 0)))
            {
                sql.Append(' ');
            }

            sql.Append(value);
            lastParenthesis = false;
        }

        public IReadOnlyList<INode> Build()
        {
            while (current < tokens.Count)
            {
                var token = tokens[current];
                switch (token.TokenType)
                {
                    case TokenType.Block:
                        AppendSql(token.Value.Trim(), true);
                        break;
                    case TokenType.OpenParenthesis:
                        AppendSql(token.Value.Trim(), true);
                        lastParenthesis = true;
                        break;
                    case TokenType.Comma:
                        AppendSql(token.Value.Trim(), false);
                        break;
                    case TokenType.CloseParenthesis:
                        AppendSql(token.Value.Trim(), false);
                        break;
                    case TokenType.Comment:
                        ParseComment(token.Value.Trim());
                        break;
                }

                current++;
            }

            Flush(false);

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
            if (value.StartsWith("%", StringComparison.Ordinal))
            {
                AddBody(new CodeNode(value[1..].Trim()), false);
            }

            // Raw
            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                SkipToken();
                AddBody(new RawSqlNode(value[1..].Trim()), true);
            }

            // Parameter
            if (value.StartsWith("@", StringComparison.Ordinal))
            {
                var hasParenthesis = SkipToken();
                AddBody(new ParameterNode(value[1..].Trim(), hasParenthesis), !lastParenthesis);
                lastParenthesis = false;
            }
        }

        private bool SkipToken()
        {
            var hasParenthesis = false;
            var token = NextToken();
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
}
