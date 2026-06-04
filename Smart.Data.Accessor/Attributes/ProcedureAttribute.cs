namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Method)]
[ExcludeFromCodeCoverage]
public sealed class ProcedureAttribute : Attribute
{
    public string Name { get; }

    public ProcedureAttribute(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }
}
