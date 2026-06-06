namespace Smart.Data.Accessor.Generator.Sql;

using System.Diagnostics.CodeAnalysis;

public enum SqlTokenizerError
{
    Unknown,
    CommentNotClosed,
    QuoteNotClosed
}

[ExcludeFromCodeCoverage]
[Serializable]
public sealed class SqlTokenizerException : Exception
{
    public SqlTokenizerError Error { get; }

    public SqlTokenizerException()
    {
    }

    public SqlTokenizerException(string message)
        : base(message)
    {
    }

    public SqlTokenizerException(SqlTokenizerError error, string message)
        : base(message)
    {
        Error = error;
    }

    public SqlTokenizerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
