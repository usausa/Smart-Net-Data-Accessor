namespace Smart.Data.Accessor.Attributes;

using System;

// Alias for TimeoutAttribute, kept for legacy parity.
[AttributeUsage(AttributeTargets.Method)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class CommandTimeoutAttribute : Attribute
{
    public int Seconds { get; }

    public CommandTimeoutAttribute(int seconds)
    {
        Seconds = seconds;
    }
}
