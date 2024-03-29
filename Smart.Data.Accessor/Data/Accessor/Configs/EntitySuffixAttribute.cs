namespace Smart.Data.Accessor.Configs;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class EntitySuffixAttribute : Attribute
{
    public string[] Values { get; }

    public EntitySuffixAttribute(params string[] values)
    {
        Values = values;
    }
}
