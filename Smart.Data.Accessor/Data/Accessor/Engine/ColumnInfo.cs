namespace Smart.Data.Accessor.Engine;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Ignore")]
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
