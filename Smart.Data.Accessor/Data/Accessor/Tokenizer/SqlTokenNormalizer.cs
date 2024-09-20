namespace Smart.Data.Accessor.Tokenizer;

public static class SqlTokenNormalizer
{
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

        var list = new List<Token>();
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
}
