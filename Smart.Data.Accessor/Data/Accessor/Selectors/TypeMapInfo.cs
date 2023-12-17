namespace Smart.Data.Accessor.Selectors;

public sealed class TypeMapInfo
{
    public Type Type { get; }

    public ConstructorMapInfo? Constructor { get; }

    public IReadOnlyList<PropertyMapInfo> Properties { get; }

    public TypeMapInfo(Type type, ConstructorMapInfo? constructor, IReadOnlyList<PropertyMapInfo> properties)
    {
        Type = type;
        Constructor = constructor;
        Properties = properties;
    }
}
