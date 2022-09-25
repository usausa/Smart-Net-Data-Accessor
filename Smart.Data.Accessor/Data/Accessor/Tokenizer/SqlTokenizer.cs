namespace Smart.Data.Accessor.Tokenizer;

public sealed class SqlTokenizer
{
    private readonly List<Token> tokens = new();

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

        return tokens;
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
            // Block comment
            current += 2;

            var start = current;
            while (current < source.Length - 1)
            {
                if ((source[current] == '*') && (source[current + 1] == '/'))
                {
                    AddToken(new Token(TokenType.Comment, source[start..current].Trim()));

                    current += 2;

                    return;
                }

                current++;
            }

            throw new SqlTokenizerException("Invalid sql. Comment is not closed.");
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
                throw new SqlTokenizerException("Invalid sql. Quote is not closed.");
            }

            AddToken(new Token(TokenType.Block, source[start..current]));
        }
        else if (source[current] == ',')
        {
            // Open
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
