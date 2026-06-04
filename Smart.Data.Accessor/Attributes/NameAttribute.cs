namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public sealed class NameAttribute : Attribute
{
    public string Name { get; }

    public NameAttribute(string name)
    {
        Name = name;
    }
}
