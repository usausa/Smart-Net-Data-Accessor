namespace Smart.Data.Accessor.Attributes;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
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
