namespace Smart.Data.Accessor.Attributes.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes.Builders.Helpers;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Nodes;

    public sealed class InsertAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public InsertAttribute()
            : this(null, null)
        {
        }

        public InsertAttribute(string table)
            : this(table, null)
        {
        }

        public InsertAttribute(Type type)
            : this(null, type)
        {
        }

        private InsertAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
            this.type = type;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(option, mi);

            // TODO sql based
            var nodes = new List<INode>
            {
                new SqlNode("INSERT INTO "),
                new SqlNode(table ?? BuildHelper.GetTableName(option, mi)),
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

                nodes.Add(new ParameterNode(parameter.Name, parameter.ParameterName));
            }

            nodes.Add(new SqlNode(")"));

            return nodes;
        }
    }
}
