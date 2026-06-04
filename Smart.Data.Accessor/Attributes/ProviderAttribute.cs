namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ProviderAttribute : Attribute
{
    public string Name { get; }

    public ProviderAttribute(string name)
    {
        Name = name;
    }
}
