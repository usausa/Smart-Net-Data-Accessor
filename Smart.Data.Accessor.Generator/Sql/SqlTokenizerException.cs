namespace Smart.Data.Accessor.Generator.Sql;

public enum SqlTokenizerErrorKind
{
    Unknown,
    CommentNotClosed,
    QuoteNotClosed
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
[Serializable]
public sealed class SqlTokenizerException : Exception
{
    public SqlTokenizerErrorKind Kind { get; }

    public SqlTokenizerException()
    {
    }

    public SqlTokenizerException(string message)
        : base(message)
    {
    }

    public SqlTokenizerException(SqlTokenizerErrorKind kind, string message)
        : base(message)
    {
        Kind = kind;
    }

    public SqlTokenizerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
