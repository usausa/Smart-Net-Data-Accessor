namespace Smart.Data.Accessor.Generator.Helpers;

internal class PathElement
{
    public string Name { get; }

    public int Indexed { get; }

    public PathElement(string name, int indexed)
    {
        Name = name;
        Indexed = indexed;
    }
}
