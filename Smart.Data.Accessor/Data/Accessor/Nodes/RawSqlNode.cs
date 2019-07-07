namespace Smart.Data.Accessor.Nodes
{
    public sealed class RawSqlNode : INode
    {
        public string Source { get; }

        public RawSqlNode(string source)
        {
            Source = source;
        }

        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
