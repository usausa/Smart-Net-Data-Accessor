namespace Smart.Data.Accessor.Attributes;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class ProviderAttribute : Attribute
{
    public object Parameter { get; }

    public ProviderAttribute(object parameter)
    {
        Parameter = parameter;
    }
}
