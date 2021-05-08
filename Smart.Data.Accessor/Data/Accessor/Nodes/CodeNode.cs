namespace Smart.Data.Accessor.Nodes
{
    public sealed class CodeNode : INode
    {
        public string Code { get; }

        public CodeNode(string code)
        {
            Code = code;
        }

        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
