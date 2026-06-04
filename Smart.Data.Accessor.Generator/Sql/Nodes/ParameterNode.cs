namespace Smart.Data.Accessor.Generator.Sql.Nodes;

public sealed class ParameterNode : NodeBase
{
    public string Name { get; }

    public bool IsMultiple { get; }

    public ParameterNode(string name, bool isMultiple)
    {
        Name = name;
        IsMultiple = isMultiple;
    }
}
