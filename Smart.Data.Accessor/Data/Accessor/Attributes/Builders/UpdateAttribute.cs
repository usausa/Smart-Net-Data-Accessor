namespace Smart.Data.Accessor.Attributes.Builders
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Nodes;

    public sealed class UpdateAttribute : MethodAttribute
    {
        private readonly string table;

        private readonly Type type;

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
            throw new System.NotImplementedException();
        }
    }
}
