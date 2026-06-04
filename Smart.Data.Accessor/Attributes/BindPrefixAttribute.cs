namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class BindPrefixAttribute : Attribute
{
    public char Marker { get; }

    public BindPrefixAttribute(char marker)
    {
        Marker = marker;
    }
}
