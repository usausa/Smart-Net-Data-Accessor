namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class MethodNameAttribute : Attribute
{
    public string Name { get; }

    public MethodNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }
}
