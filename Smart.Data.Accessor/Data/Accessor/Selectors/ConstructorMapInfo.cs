namespace Smart.Data.Accessor.Selectors;

using System.Reflection;

public class ConstructorMapInfo
{
    public ConstructorInfo Info { get; }

    public IReadOnlyList<ParameterMapInfo> Parameters { get; }

    public ConstructorMapInfo(ConstructorInfo ci, IReadOnlyList<ParameterMapInfo> parameters)
    {
        Info = ci;
        Parameters = parameters;
    }
}
