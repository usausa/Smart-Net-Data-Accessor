namespace Smart.Data.Accessor.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MethodNameAttribute : Attribute
{
    public string Name { get; }

    public MethodNameAttribute(string name)
    {
        Name = name;
    }
}
