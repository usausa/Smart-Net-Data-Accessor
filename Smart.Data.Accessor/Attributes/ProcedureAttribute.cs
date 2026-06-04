namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Method)]
public sealed class ProcedureAttribute : Attribute
{
    public string Name { get; }

    public ProcedureAttribute(string name)
    {
        Name = name;
    }
}
