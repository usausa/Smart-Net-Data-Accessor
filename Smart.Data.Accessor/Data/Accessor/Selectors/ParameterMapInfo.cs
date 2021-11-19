namespace Smart.Data.Accessor.Selectors;

using System.Reflection;

public class ParameterMapInfo
{
    public ParameterInfo Info { get; }

    public int Index { get; }

    public ParameterMapInfo(ParameterInfo pi, int index)
    {
        Info = pi;
        Index = index;
    }
}
