namespace Smart.Data.Accessor.Attributes;

using System;
using System.Diagnostics.CodeAnalysis;

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method |
    AttributeTargets.Parameter |
    AttributeTargets.ReturnValue |
    AttributeTargets.Property,
    AllowMultiple = true,
    Inherited = false)]
[SuppressMessage("Microsoft.Performance", "CA1813:Avoid unsealed attributes", Justification = "Intentionally inheritable for derived marker attributes (spec §7.4.1).")]
public class TypeHandlerAttribute<TConverter> : Attribute
{
    public Type ConverterType => typeof(TConverter);
}

[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method |
    AttributeTargets.Parameter |
    AttributeTargets.ReturnValue |
    AttributeTargets.Property,
    AllowMultiple = true,
    Inherited = false)]
public sealed class TypeHandlerAttribute : Attribute
{
    public Type ConverterType { get; }

    public TypeHandlerAttribute(Type converterType)
    {
        ConverterType = converterType;
    }
}
