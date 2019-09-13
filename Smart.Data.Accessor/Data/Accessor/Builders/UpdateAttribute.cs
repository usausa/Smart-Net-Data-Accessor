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

    public sealed class UpdateAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public bool Force { get; set; }

        public UpdateAttribute()
            : this(null, null)
        {
        }

        public UpdateAttribute(string table)
            : this(table, null)
        {
        }

        public UpdateAttribute(Type type)
            : this(null, type)
        {
        }

        private UpdateAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
            this.type = type;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, IGeneratorOption option, MethodInfo mi)
        {
            var parameters = BuildHelper.GetParameters(option, mi);
            var values = BuildHelper.GetValueParameters(parameters);
            var tableName = table ??
                            (type != null ? BuildHelper.GetTableNameOfType(option, type) : null) ??
                            BuildHelper.GetTableName(option, mi);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            var sql = new StringBuilder();
            sql.Append("UPDATE ");
            sql.Append(tableName);
            sql.Append(" SET ");

            if (values.Count > 0)
            {
                var conditions = BuildHelper.GetNonValueParameters(parameters);

                if (!Force && (conditions.Count == 0))
                {
                    throw new BuilderException($"Delete all requires force option. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
                }

                BuildHelper.AddUpdateSets(sql, mi, values);
                BuildHelper.AddCondition(sql, conditions);
            }
            else
            {
                var keys = BuildHelper.GetKeyParameters(parameters);
                if (keys.Count > 0)
                {
                    BuildHelper.AddUpdateSets(sql, mi, BuildHelper.GetNonKeyParameters(parameters));
                    BuildHelper.AddCondition(sql, keys);
                }
                else
                {
                    var conditions = BuildHelper.GetConditionParameters(parameters);

                    if (!Force && (conditions.Count == 0))
                    {
                        throw new BuilderException($"Delete all requires force option. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
                    }

                    BuildHelper.AddUpdateSets(sql, mi, BuildHelper.GetNonConditionParameters(parameters));
                    BuildHelper.AddCondition(sql, conditions);
                }
            }

            var tokenizer = new SqlTokenizer(sql.ToString());
            var builder = new NodeBuilder(tokenizer.Tokenize());
            return builder.Build();
        }
    }
}
