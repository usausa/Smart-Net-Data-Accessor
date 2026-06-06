namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

// Alias for TimeoutAttribute, kept for parity with the previous version.
[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class CommandTimeoutAttribute : Attribute
{
    public int Seconds { get; }

    public CommandTimeoutAttribute(int seconds)
    {
        Seconds = seconds;
    }
}
