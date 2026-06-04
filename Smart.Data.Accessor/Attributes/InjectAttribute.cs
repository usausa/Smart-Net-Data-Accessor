namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class InjectAttribute : Attribute
{
    public Type Type { get; }

    public string Name { get; }

    public InjectAttribute(Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(name);
        this.Type = type;
        this.Name = name;
    }
}
