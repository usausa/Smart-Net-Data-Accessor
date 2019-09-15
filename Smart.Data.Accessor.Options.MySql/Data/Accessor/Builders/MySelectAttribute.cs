namespace Smart.Data.Accessor.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using System.Text;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Builders.Helpers;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Nodes;
    using Smart.Data.Accessor.Tokenizer;

    public sealed class MySelectAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public string Order { get; set; }

        public bool ForUpdate { get; set; }

        public MySelectAttribute()
            : this(null, null)
        {
        }

        public MySelectAttribute(string table)
            : this(table, null)
        {
        }

        public MySelectAttribute(Type type)
            : this(null, type)
        {
        }

        private MySelectAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.Query)
        {
            this.table = table;
            this.type = type;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(option, mi);
            var order = BuildHelper.PickParameter<OrderAttribute>(parameters);
            var limit = BuildHelper.PickParameter<LimitAttribute>(parameters);
            var offset = BuildHelper.PickParameter<OffsetAttribute>(parameters);
            var tableName = table ??
                            (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ??
                            BuildHelper.GetReturnTableName(option, mi);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("SELECT * FROM ");
            sql.Append(tableName);
            BuildHelper.AddCondition(sql, parameters);

            if (order != null)
            {
                sql.Append(" ORDER BY ");
                sql.Append($"/*# {order.Name} */dummy");
            }
            else if (!String.IsNullOrEmpty(Order))
            {
                sql.Append(" ORDER BY ");
                sql.Append(Order);
            }
            else
            {
                var columns = BuildHelper.MakeKeyColumns(option, mi.ReturnType);
                if (!String.IsNullOrEmpty(columns))
                {
                    sql.Append(" ORDER BY ");
                    sql.Append(columns);
                }
            }

            if (limit != null)
            {
                sql.Append(" LIMIT ");
                sql.Append($"/*@ {limit.Name} */dummy");
            }

            if (offset != null)
            {
                sql.Append(" OFFSET ");
                sql.Append($"/*@ {offset.Name} */dummy");
            }

            if (ForUpdate)
            {
                sql.Append(" FOR UPDATE");
            }

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }
}
