namespace Smart.Data.Accessor.Tokenizer
{
    using System;

    [Serializable]
    public sealed class SqlTokenizerException : Exception
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
    }
}
