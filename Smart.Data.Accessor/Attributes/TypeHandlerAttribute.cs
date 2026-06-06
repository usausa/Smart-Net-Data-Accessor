namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

#pragma warning disable CA1813
[ExcludeFromCodeCoverage]
[AttributeUsage(
    AttributeTargets.Class |
    AttributeTargets.Method |
    AttributeTargets.Parameter |
    AttributeTargets.ReturnValue |
    AttributeTargets.Property,
    AllowMultiple = true,
    Inherited = false)]
public class TypeHandlerAttribute<TConverter> : Attribute
{
    public Type ConverterType => typeof(TConverter);
}
#pragma warning restore CA1813

[ExcludeFromCodeCoverage]
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
