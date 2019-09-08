namespace Smart.Data.Accessor.Attributes.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;
    using System.Text;

    using Smart.Data.Accessor.Attributes.Builders.Helpers;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Nodes;
    using Smart.Data.Accessor.Tokenizer;

    public abstract class BaseScalarAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        private readonly string field;

        protected BaseScalarAttribute(string table, Type type, string field)
            : base(CommandType.Text, MethodType.ExecuteScalar)
        {
            this.table = table;
            this.type = type;
            this.field = field;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var sql = new StringBuilder();
            sql.Append("SELECT ");
            sql.Append(field);
            sql.Append(" FROM ");
            sql.Append(table ?? (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ?? BuildHelper.GetTableName(option, mi));
            sql.Append(" WHERE ");
            BuildHelper.AddConditionNode(sql, BuildHelper.GetParameters(option, mi));

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }

    public sealed class CountAttribute : BaseScalarAttribute
    {
        private const string Field = "COUNT(*)";

        public CountAttribute()
            : base(null, null, Field)
        {
        }

        public CountAttribute(string table)
            : base(table, null, Field)
        {
        }

        public CountAttribute(Type type)
            : base(null, type, Field)
        {
        }
    }
}
