namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class MethodNameAttribute : Attribute
{
    public string Name { get; }

    public MethodNameAttribute(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        this.Name = name;
    }
}
