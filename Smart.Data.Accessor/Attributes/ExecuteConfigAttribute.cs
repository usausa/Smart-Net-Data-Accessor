namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExecuteConfigAttribute : Attribute
{
    public Type ProfileType { get; }

    public ExecuteConfigAttribute(Type profileType)
    {
        ProfileType = profileType;
    }
}
