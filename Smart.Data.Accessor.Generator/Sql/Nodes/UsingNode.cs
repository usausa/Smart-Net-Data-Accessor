namespace Smart.Data.Accessor.Generator.Sql.Nodes;

public sealed class UsingNode : NodeBase
{
    public bool IsStatic { get; }

    public string Name { get; }

    public UsingNode(bool isStatic, string name)
    {
        IsStatic = isStatic;
        Name = name;
    }
}
