namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
public sealed class ProviderAttribute : Attribute
{
    public string Name { get; }

    public ProviderAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }
}
