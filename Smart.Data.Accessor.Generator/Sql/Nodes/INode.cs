namespace Smart.Data.Accessor.Generator.Sql.Nodes;

public interface INode
{
    void Visit(INodeVisitor visitor);
}
