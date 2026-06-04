namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Alias for TimeoutAttribute, kept for legacy parity.
[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class CommandTimeoutAttribute : Attribute
{
    public int Seconds { get; }

    public CommandTimeoutAttribute(int seconds)
    {
        Seconds = seconds;
    }
}
