namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class TimeoutAttribute : Attribute
{
    public int Seconds { get; }

    public TimeoutAttribute(int seconds)
    {
        Seconds = seconds;
    }
}
