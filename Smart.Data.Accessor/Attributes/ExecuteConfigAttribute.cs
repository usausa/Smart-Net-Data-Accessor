namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class)]
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class ExecuteConfigAttribute : Attribute
{
    public Type ProfileType { get; }

    public ExecuteConfigAttribute(Type profileType)
    {
        ProfileType = profileType;
    }
}
