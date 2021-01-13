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

    public sealed class MyInsertAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public DuplicateBehavior OnDuplicate { get; set; }

        public MyInsertAttribute()
            : this(null, null)
        {
        }

        public MyInsertAttribute(string table)
            : this(table, null)
        {
        }

        public MyInsertAttribute(Type type)
            : this(null, type)
        {
        }

        private MyInsertAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
            this.type = type;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(mi);
            var values = BuildHelper.GetInsertParameters(parameters);
            var tableName = table ??
                            (type is not null ? BuildHelper.GetTableNameByType(mi, type) : null) ??
                            BuildHelper.GetTableNameByParameter(mi);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException(
                    $"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("INSERT");
            if (OnDuplicate == DuplicateBehavior.Ignore)
            {
                sql.Append(" IGNORE");
            }
            sql.Append(" INTO ");
            sql.Append(tableName);
            sql.Append(" (");
            BuildHelper.AddInsertColumns(sql, mi, values);
            sql.Append(") VALUES (");
            BuildHelper.AddInsertValues(sql, mi, values);
            sql.Append(')');

            if (OnDuplicate == DuplicateBehavior.Update)
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
