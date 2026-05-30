namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class BindPrefixAttribute : Attribute
{
    public char Marker { get; }

    public BindPrefixAttribute(char marker)
    {
        this.Marker = marker;
    }
}
