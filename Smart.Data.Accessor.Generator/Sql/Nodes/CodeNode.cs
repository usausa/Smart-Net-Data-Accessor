namespace Smart.Data.Accessor.Generator.Sql.Nodes;

public sealed class CodeNode : NodeBase
{
    public string Code { get; }

    public CodeNode(string code)
    {
        Code = code;
    }
}
