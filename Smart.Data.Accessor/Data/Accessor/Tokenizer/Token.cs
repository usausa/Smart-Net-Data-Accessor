namespace Smart.Data.Accessor.Tokenizer;

public sealed class Token
{
    public TokenType TokenType { get; }

    public string Value { get; }

    public Token(TokenType tokenType, string value)
    {
        TokenType = tokenType;
        Value = value;
    }
}
