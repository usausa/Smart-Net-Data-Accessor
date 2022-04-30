namespace Smart.Data.Accessor.Generator.Metadata;

using System.Data;

internal sealed class ParameterEntry
{
    public int Index { get; }

    public string Name { get; }

    public string? ParameterName { get; }

    public string Source { get; }

    public Type Type { get; }

    public ParameterDirection Direction { get; }

    public bool IsMultiple { get; }

    public int ParameterIndex { get; }

    public Type? DeclaringType { get; }

    public string? PropertyName { get; }

    public ParameterEntry(
        int index,
        string name,
        string? parameterName,
        string source,
        Type type,
        ParameterDirection direction,
        bool isMultiple,
        int parameterIndex,
        Type? declaringType,
        string? propertyName)
    {
        Index = index;
        Name = name;
        ParameterName = parameterName;
        Source = source;
        Type = type;
        Direction = direction;
        IsMultiple = isMultiple;
        ParameterIndex = parameterIndex;
        DeclaringType = declaringType;
        PropertyName = propertyName;
    }
}
