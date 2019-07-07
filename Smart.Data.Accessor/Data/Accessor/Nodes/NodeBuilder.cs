namespace Smart.Data.Accessor.Nodes
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using Smart.Data.Accessor.Tokenizer;

    public sealed class NodeBuilder
    {
        private readonly IReadOnlyList<Token> tokens;

        private readonly List<INode> pragmaNodes = new List<INode>();

        private readonly List<INode> bodyNodes = new List<INode>();

        private readonly StringBuilder sql = new StringBuilder();

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
                    sql.Append(" ");
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
                sql.Append(" ");
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
            if (value.StartsWith("!helper"))
            {
                AddPragmaNode(new UsingNode(true, value.Substring(7).Trim()));
            }

            if (value.StartsWith("!using"))
            {
                AddPragmaNode(new UsingNode(false, value.Substring(6).Trim()));
            }

            // Code
            if (value.StartsWith("%"))
            {
                AddBody(new CodeNode(value.Substring(1).Trim()), false);
            }

            // Raw
            if (value.StartsWith("#"))
            {
                AddBody(new RawSqlNode(value.Substring(1).Trim()), true);
            }

            // Parameter
            if (value.StartsWith("@"))
            {
                AddBody(new ParameterNode(value.Substring(1).Trim()), true);

                var token = NextToken();
                if (token != null)
                {
                    if (token.TokenType == TokenType.OpenParenthesis)
                    {
                        var count = 1;
                        while ((count > 0) && ((token = NextToken()) != null))
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
            }
        }
    }
}
