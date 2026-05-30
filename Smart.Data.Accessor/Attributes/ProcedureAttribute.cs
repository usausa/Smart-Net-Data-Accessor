namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ProcedureAttribute : Attribute
{
    public string Name { get; }

    public ProcedureAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        this.Name = name;
    }
}
