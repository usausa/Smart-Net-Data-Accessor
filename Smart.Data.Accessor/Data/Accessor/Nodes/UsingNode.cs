namespace Smart.Data.Accessor.Nodes
{
    public sealed class UsingNode : INode
    {
        public bool IsStatic { get; }

        public string Name { get; }

        public UsingNode(bool isStatic, string name)
        {
            IsStatic = isStatic;
            Name = name;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
