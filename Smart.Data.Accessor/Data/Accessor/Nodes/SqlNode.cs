namespace Smart.Data.Accessor.Nodes
{
    public sealed class SqlNode : INode
    {
        public string Sql { get; }

        public SqlNode(string sql)
        {
            Sql = sql;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public void Visit(INodeVisitor visitor) => visitor.Visit(this);
    }
}
