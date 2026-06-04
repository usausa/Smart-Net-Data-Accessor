namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class TimeoutAttribute : Attribute
{
    public int Seconds { get; }

    public TimeoutAttribute(int seconds)
    {
        Seconds = seconds;
    }
}
