namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class NameAttribute : Attribute
{
    public string Name { get; }

    public NameAttribute(string name)
    {
        Name = name;
    }
}
