namespace Smart.Data.Accessor.Attributes;

using System;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConverterSupportedTypesAttribute : Attribute
{
    public Type[] Types { get; }

    public ConverterSupportedTypesAttribute(params Type[] types)
    {
        Types = types;
    }
}
