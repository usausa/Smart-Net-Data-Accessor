namespace Smart.Data.Accessor.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandTimeoutAttribute : Attribute
{
    public int Timeout { get; }

    public CommandTimeoutAttribute(int timeout)
    {
        Timeout = timeout;
    }
}
