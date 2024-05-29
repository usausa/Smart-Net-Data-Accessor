namespace Smart.Data.Accessor.Nodes;

public sealed class SqlNode : INode
{
    public string Sql { get; }

    public SqlNode(string sql)
    {
        Sql = sql;
    }

    public void Visit(INodeVisitor visitor) => visitor.Visit(this);
}
