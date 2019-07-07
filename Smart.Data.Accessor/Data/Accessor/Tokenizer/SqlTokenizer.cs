namespace Smart.Data.Accessor.Tokenizer
{
    using System;
    using System.Collections.Generic;

    public sealed class SqlTokenizer
    {
        private readonly List<Token> tokens = new List<Token>();

        private readonly string source;

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

        private void Peek2Chars()
        {
            if ((source[current] == '\r') && (source[current + 1] == '\n'))
            {
                // EOL
                current += 2;
            }
            else if ((source[current] == '-') && (source[current + 1] == '-'))
            {
                // Line comment
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
                        tokens.Add(new Token(TokenType.Comment, source.Substring(start, current - start).Trim()));

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
                current += 1;
            }
            else if (Char.IsWhiteSpace(source[current]))
            {
                // Space
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

                tokens.Add(new Token(TokenType.Block, source.Substring(start, current - start)));
            }
            else if (source[current] == '(')
            {
                // Open
                tokens.Add(new Token(TokenType.OpenParenthesis, "("));
                current += 1;
            }
            else if (source[current] == ')')
            {
                // Close
                tokens.Add(new Token(TokenType.CloseParenthesis, ")"));
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

                tokens.Add(new Token(TokenType.Block, source.Substring(start, current - start)));
            }
        }

        private static bool IsBlockChar(char c)
        {
            if (Char.IsWhiteSpace(c))
            {
                return false;
            }

            switch (c)
            {
                case '\'':
                case '(':
                case ')':
                case '\r':
                case '\n':
                case '/':
                case '-':
                    return false;
            }

            return true;
        }
    }
}
