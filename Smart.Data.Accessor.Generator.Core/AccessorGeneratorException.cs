namespace Smart.Data.Accessor.Generator;

[Serializable]
public sealed class AccessorGeneratorException : Exception
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
}
