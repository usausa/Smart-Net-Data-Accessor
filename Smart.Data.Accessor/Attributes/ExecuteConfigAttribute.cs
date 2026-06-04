namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
public sealed class ExecuteConfigAttribute : Attribute
{
    public Type ProfileType { get; }

    public ExecuteConfigAttribute(Type profileType)
    {
        ProfileType = profileType;
    }
}
