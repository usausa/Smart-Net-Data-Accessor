namespace Smart.Data.Accessor.Generator.Sql;

public sealed class SqlTokenizer
{
    private readonly List<Token> tokens = [];

    private readonly string source;

    private bool blank;

    private int current;

    public SqlTokenizer(string source)
    {
        this.source = source;
    }

    public IReadOnlyList<Token> Tokenize()
    {
        int remain;
        while ((remain = source.Length - current) > 0)
        {
            if (remain >= 2)
            {
                Peek2Chars();
            }
            else if (remain >= 1)
            {
                Peek1Chars();
            }
        }

        return Normalize(tokens);
    }

    public static IReadOnlyList<Token> Normalize(IReadOnlyList<Token> source)
    {
        // Trim start blank
        var start = 0;
        while ((start < source.Count) && (source[start].TokenType == TokenType.Blank))
        {
            start++;
        }

        // End start blank
        var end = source.Count - 1;
        while ((end >= 0) && (source[end].TokenType == TokenType.Blank))
        {
            end--;
        }

        var capacity = end - start + 1;
        var list = capacity > 0 ? new List<Token>(capacity) : [];
        for (var i = start; i <= end; i++)
        {
            var token = source[i];

            var skip = false;
            if (token.TokenType == TokenType.Blank)
            {
                var findLast = TryFindLastTokenTypeWithoutComment(list, out var lastToken);
                if (findLast && (lastToken == TokenType.OpenParenthesis))
                {
                    // Last is open
                    skip = true;
                }
                else if (findLast && (lastToken == TokenType.Blank))
                {
                    // Last is blank
                    skip = true;
                }
                else if ((i < end) && (source[i + 1].TokenType == TokenType.CloseParenthesis))
                {
                    // Next is close
                    skip = true;
                }
            }

            if (!skip)
            {
                list.Add(token);
            }
        }

        return list;

        static bool TryFindLastTokenTypeWithoutComment(IReadOnlyList<Token> source, out TokenType tokenType)
        {
            for (var i = source.Count - 1; i >= 0; i--)
            {
                tokenType = source[i].TokenType;
                if (tokenType != TokenType.Comment)
                {
                    return true;
                }
            }

            tokenType = default;
            return false;
        }
    }

    private void AddToken(Token token)
    {
        if (blank)
        {
            tokens.Add(new Token(TokenType.Blank, " "));
        }

        tokens.Add(token);
        blank = false;
    }

    private void Peek2Chars()
    {
        if ((source[current] == '\r') && (source[current + 1] == '\n'))
        {
            // EOL
            blank = true;
            current += 2;
        }
        else if ((source[current] == '-') && (source[current + 1] == '-'))
        {
            // Line comment
            blank = true;
            current += 2;

            int remain;
            while ((remain = source.Length - current) > 0)
            {
                if ((remain >= 2) && (source[current] == '\r') && (source[current + 1] == '\n'))
                {
                    current += 2;
                    return;
                }
                if ((remain >= 1) && ((source[current] == '\r') || (source[current] == '\n')))
                {
                    current += 1;
                    return;
                }

                current++;
            }
        }
        else if ((source[current] == '/') && (source[current + 1] == '*'))
        {
            // Block comment — classify by first non-whitespace marker inside the body:
            //   '+'                        → query hint, emit Token(Hint, body) inline
            //   '!' / '@' / '#' / '%'      → 2-way SQL directive, emit Token(Comment, ...)
            //   anything else (incl. empty) → plain comment, drop the token but set `blank`
            //                                so adjacent SQL tokens still get separated.
            current += 2;

            var start = current;
            while (current < source.Length - 1)
            {
                if ((source[current] == '*') && (source[current + 1] == '/'))
                {
                    var raw = source[start..current];
                    current += 2;

                    var trimmed = raw.Trim();
                    if (trimmed.Length == 0)
                    {
                        blank = true;
                        return;
                    }

                    var marker = trimmed[0];
                    if (marker == '+')
                    {
                        AddToken(new Token(TokenType.Hint, trimmed[1..].Trim()));
                        return;
                    }

                    if (marker is '!' or '@' or '#' or '%')
                    {
                        AddToken(new Token(TokenType.Comment, trimmed));
                        return;
                    }

                    // Plain comment: skip the comment, but ensure surrounding tokens stay separated.
                    blank = true;
                    return;
                }

                current++;
            }

            throw new SqlTokenizerException(SqlTokenizerErrorKind.CommentNotClosed, "Invalid sql. Comment is not closed.");
        }
        else
        {
            Peek1Chars();
        }
    }

    private void Peek1Chars()
    {
        if ((source[current] == '\r') || (source[current] == '\n'))
        {
            // EOL
            blank = true;
            current += 1;
        }
        else if (Char.IsWhiteSpace(source[current]))
        {
            // Space
            blank = true;
            current += 1;
        }
        else if (source[current] == '\'')
        {
            // Quote
            var start = current;
            current++;

            var closed = false;
            while (current < source.Length && !closed)
            {
                if (source[current] == '\'')
                {
                    current++;

                    if ((current < source.Length) && (source[current] == '\''))
                    {
                        current++;
                    }
                    else
                    {
                        closed = true;
                    }
                }
                else
                {
                    current++;
                }
            }

            if (!closed)
            {
                throw new SqlTokenizerException(SqlTokenizerErrorKind.QuoteNotClosed, "Invalid sql. Quote is not closed.");
            }

            AddToken(new Token(TokenType.Block, source[start..current]));
        }
        else if (source[current] == ',')
        {
            // Comma
            AddToken(new Token(TokenType.Comma, ","));
            current += 1;
        }
        else if (source[current] == '(')
        {
            // Open
            AddToken(new Token(TokenType.OpenParenthesis, "("));
            current += 1;
        }
        else if (source[current] == ')')
        {
            // Close
            AddToken(new Token(TokenType.CloseParenthesis, ")"));
            current += 1;
        }
        else
        {
            // Block
            var start = current;
            current++;

            while ((current < source.Length) && IsBlockChar(source[current]))
            {
                current++;
            }

            AddToken(new Token(TokenType.Block, source[start..current]));
        }
    }

    private static bool IsBlockChar(char c)
    {
        if (Char.IsWhiteSpace(c))
        {
            return false;
        }

        return c switch
        {
            '\'' => false,
            ',' => false,
            '(' => false,
            ')' => false,
            '\r' => false,
            '\n' => false,
            '/' => false,
            '-' => false,
            _ => true
        };
    }
}
