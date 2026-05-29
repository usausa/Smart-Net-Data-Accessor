namespace Smart.Data.Accessor.Attributes;

using System;

// Overrides the SQL file lookup name when explicit naming is required.
[AttributeUsage(AttributeTargets.Method)]
public sealed class MethodNameAttribute : Attribute
{
    public string Name { get; }

    public MethodNameAttribute(string name)
    {
        Name = name;
    }
}
