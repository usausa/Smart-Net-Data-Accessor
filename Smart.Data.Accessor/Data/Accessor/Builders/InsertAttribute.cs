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
            var tableName = table ??
                            (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ??
                            BuildHelper.GetTableName(option, mi);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("INSERT INTO ");
            sql.Append(tableName);
            sql.Append(" (");

            var add = false;
            for (var i = 0; i < parameters.Count; i++)
            {
                BuildHelper.AddSplitter(sql, add);
                add = true;

                sql.Append(parameters[i].ParameterName);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalDbValueAttribute>())
            {
                BuildHelper.AddSplitter(sql, add);
                add = true;

                sql.Append(attribute.Column);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalCodeValueAttribute>())
            {
                BuildHelper.AddSplitter(sql, add);
                add = true;

                sql.Append(attribute.Column);
            }

            sql.Append(") VALUES (");

            add = false;
            foreach (var parameter in parameters)
            {
                BuildHelper.AddSplitter(sql, add);
                add = true;

                BuildHelper.AddParameter(sql, parameter, Operation.Insert);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalDbValueAttribute>())
            {
                BuildHelper.AddSplitter(sql, add);
                add = true;

                BuildHelper.AddDbParameter(sql, attribute.Value);
            }

            foreach (var attribute in mi.GetCustomAttributes<AdditionalCodeValueAttribute>())
            {
                BuildHelper.AddSplitter(sql, add);
                add = true;

                BuildHelper.AddCodeParameter(sql, attribute.Value);
            }

            sql.Append(")");

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }
}
