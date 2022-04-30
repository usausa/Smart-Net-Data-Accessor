namespace Smart.Data.Accessor.Attributes;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public sealed class InjectAttribute : Attribute
{
    public Type Type { get; }

    public string Name { get; }

    public InjectAttribute(Type type, string name)
    {
        Type = type;
        Name = name;
    }
}
