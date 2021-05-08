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

    public sealed class InsertAttribute : MethodAttribute
    {
        private readonly string? table;

        private readonly Type? type;

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

        private InsertAttribute(string? table, Type? type)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
            this.type = type;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(mi);
            var values = BuildHelper.GetInsertParameters(parameters);
            var tableName = table ??
                            (type is not null ? BuildHelper.GetTableNameByType(mi, type) : null) ??
                            BuildHelper.GetTableNameByParameter(mi);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType!.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("INSERT INTO ");
            sql.Append(tableName);
            sql.Append(" (");
            BuildHelper.AddInsertColumns(sql, mi, values);
            sql.Append(") VALUES (");
            BuildHelper.AddInsertValues(sql, mi, values);
            sql.Append(')');

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }
}
