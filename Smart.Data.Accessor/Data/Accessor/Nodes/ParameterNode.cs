namespace Smart.Data.Accessor.Nodes
{
    public sealed class ParameterNode : INode
    {
        public string Source { get; }

        public string ParameterName { get; }

        public ParameterNode(string source)
            : this(source, null)
        {
        }

        public ParameterNode(string source, string parameterName)
        {
            Source = source;
            ParameterName = parameterName;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
