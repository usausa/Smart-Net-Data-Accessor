namespace Smart.Data.Accessor.Engine;

#pragma warning disable CA1815
public readonly struct ColumnInfo
{
    public string Name { get; }

    public Type Type { get; }

    public ColumnInfo(string name, Type type)
    {
        Name = name;
        Type = type;
    }
}
#pragma warning restore CA1815
