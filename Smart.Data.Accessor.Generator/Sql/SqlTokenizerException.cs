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

    // エラー箇所の SQL 文字列内オフセット(不明なら -1)。[Sql] インライン SQL では診断位置を
    // リテラル内の該当箇所へ割り出すために使う。
    // Character offset of the error inside the SQL text (-1 when unknown). Used by [Sql] inline SQL
    // to point the diagnostic at the exact spot inside the literal.
    public int Position { get; } = -1;

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

    public SqlTokenizerException(SqlTokenizerError error, int position, string message)
        : base(message)
    {
        Error = error;
        Position = position;
    }

    public SqlTokenizerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
