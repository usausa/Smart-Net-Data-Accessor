namespace Smart.Data.Accessor.Attributes
{
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Nodes;

    public sealed class DeleteAttribute : MethodAttribute
    {
        private readonly string table;

        public DeleteAttribute()
            : this(null)
        {
        }

        public DeleteAttribute(string table)
            : base(CommandType.Text, MethodType.Execute)
        {
            this.table = table;
        }

        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
        {
            throw new System.NotImplementedException();
        }
    }
}
