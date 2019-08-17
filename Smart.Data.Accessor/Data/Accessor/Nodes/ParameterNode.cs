namespace Smart.Data.Accessor.Nodes
{
    public sealed class ParameterNode : INode
    {
        public string Name { get; }

        public string ParameterName { get; }

        public ParameterNode(string name)
            : this(name, null)
        {
        }

        public ParameterNode(string name, string parameterName)
        {
            Name = name;
            ParameterName = parameterName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
