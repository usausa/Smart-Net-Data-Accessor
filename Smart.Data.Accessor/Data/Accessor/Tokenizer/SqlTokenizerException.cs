namespace Smart.Data.Accessor.Tokenizer;

using System;
using System.Runtime.Serialization;

[Serializable]
public class SqlTokenizerException : Exception
{
    public SqlTokenizerException()
    {
    }

    public SqlTokenizerException(string message)
        : base(message)
    {
    }

    public SqlTokenizerException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected SqlTokenizerException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
