namespace Smart.Data.Accessor.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Builders.Helpers;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Nodes;

    public sealed class TruncateAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

        public TruncateAttribute(string table)
            : this(table, null)
        {
        }

        public TruncateAttribute(Type type)
            : this(null, type)
        {
        }

        private TruncateAttribute(string table, Type type)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
            this.type = type;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
        {
            var tableName = table ??
                            (type != null ? BuildHelper.GetTableNameByType(mi, type) : null);

            if (String.IsNullOrEmpty(tableName))
            {
                throw new BuilderException($"Table name resolve failed. type=[{mi.DeclaringType.FullName}], method=[{mi.Name}]");
            }

            return new[]
            {
                new SqlNode($"TRUNCATE TABLE {tableName}")
            };
        }
    }
}
