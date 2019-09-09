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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(option, mi);
            var keys = BuildHelper.GetKeyParameters(parameters);
            var tableName = table ??
                            (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ??
                            BuildHelper.GetTableName(option, mi);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("SELECT ");
            sql.Append(field);
            sql.Append(" FROM ");
            sql.Append(tableName);
            BuildHelper.AddCondition(sql, keys.Count > 0 ? keys : parameters);

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
