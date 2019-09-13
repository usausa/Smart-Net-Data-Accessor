namespace Smart.Data.Accessor.Builders.MySql
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

        public bool Ignore { get; set; }

        public bool OrUpdate { get; set; }

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(option, mi);
            var tableName = table ??
                            (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ??
                            BuildHelper.GetTableName(option, mi);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException(
                    $"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("INSERT");
            if (Ignore)
            {
                sql.Append(" IGNORE");
            }
            sql.Append("INSERT IGNORE INTO ");
            sql.Append(" INTO ");
            sql.Append(tableName);
            sql.Append(" (");
            BuildHelper.AddInsertColumns(sql, mi, parameters);
            sql.Append(") VALUES (");
            BuildHelper.AddInsertValues(sql, mi, parameters);
            sql.Append(")");

            if (OrUpdate)
            {
                var keys = BuildHelper.GetKeyParameters(parameters);
                if (keys.Count == 0)
                {
                    throw new BuilderException($"Insert or Update requires key columns. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
                }

                sql.Append(" ON DUPLICATE KEY UPDATE ");
                BuildHelper.AddUpdateSets(sql, mi, BuildHelper.GetNonKeyParameters(parameters));
            }

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }
}
