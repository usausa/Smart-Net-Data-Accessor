namespace Smart.Data.Accessor.Generator.Sql.Nodes;

public sealed class RawSqlNode : NodeBase
{
    public string Source { get; }

    public RawSqlNode(string source)
    {
        Source = source;
    }
}
