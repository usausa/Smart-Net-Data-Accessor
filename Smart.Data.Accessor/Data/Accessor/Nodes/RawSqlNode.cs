namespace Smart.Data.Accessor.Nodes
{
    public sealed class RawSqlNode : INode
    {
        public string Source { get; }

        public RawSqlNode(string source)
        {
            Source = source;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
