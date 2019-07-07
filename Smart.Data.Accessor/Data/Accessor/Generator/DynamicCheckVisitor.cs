namespace Smart.Data.Accessor.Generator
{
    using Smart.Data.Accessor.Nodes;

    public sealed class DynamicCheckVisitor : NodeVisitorBase
    {
        public bool IsDynamic { get; private set; }

        public override void Visit(RawSqlNode node) => IsDynamic = true;

        public override void Visit(CodeNode node) => IsDynamic = true;
    }
}
