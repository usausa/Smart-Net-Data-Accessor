namespace Smart.Data.Accessor.Attributes;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class OptimizeAttribute : Attribute
{
    public bool Value { get; }

    public OptimizeAttribute(bool value)
    {
        Value = value;
    }
}
