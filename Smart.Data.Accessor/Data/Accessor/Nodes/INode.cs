namespace Smart.Data.Accessor.Nodes
{
    public interface INode
    {
        void Visit(INodeVisitor visitor);
    }
}
