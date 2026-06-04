namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Method)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class TimeoutAttribute : Attribute
{
    public int Seconds { get; }

    public TimeoutAttribute(int seconds)
    {
        Seconds = seconds;
    }
}
