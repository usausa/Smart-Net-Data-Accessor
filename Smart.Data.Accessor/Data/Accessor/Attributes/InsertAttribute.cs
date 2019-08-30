namespace Smart.Data.Accessor.Attributes
{
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Builders;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Nodes;

    public sealed class InsertAttribute : MethodAttribute
    {
        private readonly string table;

        public InsertAttribute()
            : this(null)
        {
        }

        public InsertAttribute(string table)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var parameters = BuildHelper.CreateParameterNodes(mi);

            var nodes = new List<INode>
            {
                new SqlNode("INSERT INTO "),
                new SqlNode(table ?? BuildHelper.GetTableName(mi, option)),
                new SqlNode(" (")
            };

            var first = true;
            foreach (var parameter in parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    nodes.Add(new SqlNode(", "));
                }

                nodes.Add(new SqlNode(parameter.ParameterName));
            }

            nodes.Add(new SqlNode(") VALUES ("));

            first = true;
            foreach (var parameter in parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    nodes.Add(new SqlNode(", "));
                }

                nodes.Add(parameter);
            }

            nodes.Add(new SqlNode(")"));

            return nodes;
        }
    }
}
