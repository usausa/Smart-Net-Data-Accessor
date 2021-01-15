namespace Smart.Data.Accessor.Nodes
{
    public interface INodeVisitor
    {
        void Visit(UsingNode node);

        void Visit(SqlNode node);

        void Visit(RawSqlNode node);

        void Visit(CodeNode node);

        void Visit(ParameterNode node);
    }
}
