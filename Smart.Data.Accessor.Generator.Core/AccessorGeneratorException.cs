namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Runtime.Serialization;

    [Serializable]
    public class AccessorGeneratorException : Exception
    {
        public AccessorGeneratorException()
        {
        }

        public AccessorGeneratorException(string message)
            : base(message)
        {
        }

        public AccessorGeneratorException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected AccessorGeneratorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
