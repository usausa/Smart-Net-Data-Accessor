namespace Smart.Data.Accessor.Builders;

[Serializable]
public class BuilderException : Exception
{
    public BuilderException()
    {
    }

    public BuilderException(string message)
        : base(message)
    {
    }

    public BuilderException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
