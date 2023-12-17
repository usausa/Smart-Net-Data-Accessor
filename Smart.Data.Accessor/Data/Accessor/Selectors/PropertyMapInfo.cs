namespace Smart.Data.Accessor.Selectors;

using System.Reflection;

public sealed class PropertyMapInfo
{
    public PropertyInfo Info { get; }

    public int Index { get; }

    public PropertyMapInfo(PropertyInfo pi, int index)
    {
        Info = pi;
        Index = index;
    }
}
