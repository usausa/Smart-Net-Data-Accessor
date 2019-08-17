namespace Smart.Data.Accessor.Generator.Visitors
{
    using System;
    using System.Linq;

    using Smart.Data.Accessor.Generator.Metadata;
    using Smart.Data.Accessor.Nodes;

    internal sealed class CalcSizeVisitor : NodeVisitorBase
    {
        private readonly MethodMetadata mm;

        private int size;

        private int args;

        public int InitialSize => (int)(Math.Ceiling((double)size / 32) * 32);

        public CalcSizeVisitor(MethodMetadata mm)
        {
            this.mm = mm;
        }

        public override void Visit(SqlNode node) => size += node.Sql.Length;

        public override void Visit(RawSqlNode node) => size += 16;

        public override void Visit(ParameterNode node)
        {
            var parameterSize = (int)Math.Log10(++args) + 2;

            var parameter = mm.FindParameterByName(node.Name);
            if (parameter.ParameterType == ParameterType.Simple)
            {
                size += parameterSize;
            }
            else
            {
                size += (parameterSize + 4) * 8;
            }
        }
    }
}
