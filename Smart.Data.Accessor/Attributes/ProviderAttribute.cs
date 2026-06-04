namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ProviderAttribute : Attribute
{
    public string Name { get; }

    public ProviderAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        this.Name = name;
    }
}
