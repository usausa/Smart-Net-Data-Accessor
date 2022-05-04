namespace Smart.Data.Accessor.Attributes;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Method)]
public sealed class BindPrefixAttribute : Attribute
{
    public char Value { get; }

    public BindPrefixAttribute(char value)
    {
        Value = value;
    }
}
