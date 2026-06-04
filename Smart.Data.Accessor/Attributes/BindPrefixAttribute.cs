namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class BindPrefixAttribute : Attribute
{
    public char Marker { get; }

    public BindPrefixAttribute(char marker)
    {
        Marker = marker;
    }
}
