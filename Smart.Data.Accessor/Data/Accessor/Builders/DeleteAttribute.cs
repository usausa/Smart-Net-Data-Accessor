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

    public sealed class DeleteAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public bool Force { get; set; }

        public DeleteAttribute()
            : this(null, null)
        {
        }

        public DeleteAttribute(string table)
            : this(table, null)
        {
        }

        public DeleteAttribute(Type type)
            : this(null, type)
        {
        }

        private DeleteAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
            this.type = type;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(option, mi);
            var keys = BuildHelper.GetKeyParameters(parameters);
            var tableName = table ??
                            (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ??
                            BuildHelper.GetTableName(option, mi);
            var conditions = keys.Count > 0 ? keys : parameters;

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            if (!Force && (conditions.Count == 0))
            {
                throw new BuilderException($"Delete all requires force option. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("DELETE FROM ");
            sql.Append(tableName);
            BuildHelper.AddCondition(sql, conditions);

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }
}
