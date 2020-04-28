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

    public sealed class PgSelectSingleAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public bool ForUpdate { get; set; }

        public PgSelectSingleAttribute()
            : this(null, null)
        {
        }

        public PgSelectSingleAttribute(string table)
            : this(table, null)
        {
        }

        public PgSelectSingleAttribute(Type type)
            : this(null, type)
        {
        }

        private PgSelectSingleAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.QueryFirstOrDefault)
        {
            this.table = table;
            this.type = type;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(mi);
            var keys = BuildHelper.GetKeyParameters(parameters);
            var tableType = type ?? BuildHelper.GetTableTypeByReturn(mi);
            var tableName = table ??
                            (tableType != null ? BuildHelper.GetTableNameByType(mi, tableType) : null);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("SELECT * FROM ");
            sql.Append(tableName);
            BuildHelper.AddCondition(sql, keys.Count > 0 ? keys : parameters);

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
