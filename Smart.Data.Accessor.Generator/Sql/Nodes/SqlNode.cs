namespace Smart.Data.Accessor.Generator.Sql.Nodes;

public sealed class SqlNode : NodeBase
{
    public string Sql { get; }

    public SqlNode(string sql)
    {
        Sql = sql;
    }
}
