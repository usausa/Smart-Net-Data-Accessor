namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[ExcludeFromCodeCoverage]
public sealed class InjectAttribute : Attribute
{
    public Type Type { get; }

    public string Name { get; }

    public InjectAttribute(Type type, string name)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(name);
        Type = type;
        Name = name;
    }
}
