namespace Smart.Data.Accessor;

using System;
using System.Runtime.Serialization;

[Serializable]
public class AccessorRuntimeException : Exception
{
    public AccessorRuntimeException()
    {
    }

    public AccessorRuntimeException(string message)
        : base(message)
    {
    }

    public AccessorRuntimeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    protected AccessorRuntimeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
