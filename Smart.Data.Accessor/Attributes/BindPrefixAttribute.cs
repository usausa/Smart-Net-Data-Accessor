namespace Smart.Data.Accessor.Attributes;

using System;

// Marker for parameter prefix when binding object members (e.g. entity properties)
// to SQL parameters.
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class BindPrefixAttribute : Attribute
{
    public string Prefix { get; }

    public BindPrefixAttribute(string prefix)
    {
        Prefix = prefix;
    }
}
