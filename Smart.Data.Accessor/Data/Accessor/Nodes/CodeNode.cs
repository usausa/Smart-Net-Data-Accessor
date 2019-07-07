namespace Smart.Data.Accessor.Nodes
{
    public sealed class CodeNode : INode
    {
        public string Code { get; }

        public CodeNode(string code)
        {
            Code = code;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
