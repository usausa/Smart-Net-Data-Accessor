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

    public abstract class BaseSelectAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        protected BaseSelectAttribute(string table, Type type, MethodType methodType)
            : base(CommandType.Text, methodType)
        {
            this.table = table;
            this.type = type;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var sql = new StringBuilder();
            sql.Append("SELECT * FROM ");
            sql.Append(table ?? (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ?? BuildHelper.GetReturnTableName(option, mi));
            sql.Append(" WHERE ");
            BuildHelper.AddConditionNode(sql, BuildHelper.GetParameters(option, mi));

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }

    public sealed class SelectAttribute : BaseSelectAttribute
    {
        public SelectAttribute()
            : base(null, null, MethodType.Query)
        {
        }

        public SelectAttribute(string table)
            : base(table, null, MethodType.Query)
        {
        }

        public SelectAttribute(Type type)
            : base(null, type, MethodType.Query)
        {
        }
    }

    public sealed class SelectSingleAttribute : BaseSelectAttribute
    {
        public SelectSingleAttribute()
            : base(null, null, MethodType.QueryFirstOrDefault)
        {
        }

        public SelectSingleAttribute(string table)
            : base(table, null, MethodType.QueryFirstOrDefault)
        {
        }

        public SelectSingleAttribute(Type type)
            : base(null, type, MethodType.QueryFirstOrDefault)
        {
        }
    }
}
