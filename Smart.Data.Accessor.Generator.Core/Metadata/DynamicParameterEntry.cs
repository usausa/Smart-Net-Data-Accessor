namespace Smart.Data.Accessor.Generator.Metadata;

internal sealed class DynamicParameterEntry
{
    public string Name { get; }

    public int Index { get; }

    public bool IsMultiple { get; }

    public DynamicParameterEntry(string name, int index, bool isMultiple)
    {
        Name = name;
        Index = index;
        IsMultiple = isMultiple;
    }
}
