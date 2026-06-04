namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class MethodNameAttribute : Attribute
{
    public string Name { get; }

    public MethodNameAttribute(string name)
    {
        Name = name;
    }
}
