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

    public sealed class SqlMergeAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public SqlMergeAttribute()
            : this(null, null)
        {
        }

        public SqlMergeAttribute(string table)
            : this(table, null)
        {
        }

        public SqlMergeAttribute(Type type)
            : this(null, type)
        {
        }

        private SqlMergeAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
            this.type = type;
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

            if (keys.Count == 0)
            {
                throw new BuilderException($"Merge requires any keys. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("MERGE INTO ");
            sql.Append(tableName);
            sql.Append(" _T0 USING (SELECT ");

            for (var i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(", ");
                }

                BuildHelper.AddBindParameter(sql, keys[i]);
                sql.Append(" AS ");
                sql.Append(keys[i].ParameterName);
            }

            sql.Append(") AS _T1 ON (");

            for (var i = 0; i < keys.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(" AND ");
                }

                sql.Append($"_T0.{keys[i].ParameterName} = _T1.{keys[i].ParameterName}");
            }

            sql.Append(") ");

            sql.Append("WHEN NOT MATCHED THEN INSERT (");
            BuildHelper.AddInsertColumns(sql, mi, parameters);
            sql.Append(") VALUES (");
            BuildHelper.AddInsertValues(sql, mi, parameters);
            sql.Append(") ");

            sql.Append("WHEN MATCHED THEN UPDATE SET ");
            BuildHelper.AddUpdateSets(sql, mi, BuildHelper.GetNonKeyParameters(parameters));

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }
}
