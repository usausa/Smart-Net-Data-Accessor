namespace Smart.Data.Accessor.Nodes
{
    public sealed class ParameterNode : INode
    {
        public string Name { get; }

        public string ParameterName { get; }

        public bool IsMultiple { get; }

        public ParameterNode(string name)
            : this(name, null, false)
        {
        }

        public ParameterNode(string name, bool isMultiple)
            : this(name, null, isMultiple)
        {
        }

        public ParameterNode(string name, string parameterName)
            : this(name, parameterName, false)
        {
        }

        public ParameterNode(string name, string parameterName, bool isMultiple)
        {
            Name = name;
            ParameterName = parameterName;
            IsMultiple = isMultiple;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
