namespace Smart.Data.Accessor.Attributes
{
    using System.Collections.Generic;
    using System.Data;
    using System.Reflection;

    using Smart.Data.Accessor.Loaders;
    using Smart.Data.Accessor.Nodes;
    using Smart.Data.Accessor.Tokenizer;

    public abstract class LoaderMethodAttribute : MethodAttribute
    {
        protected LoaderMethodAttribute(MethodType methodType)
            : base(CommandType.Text, methodType)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods", Justification = "Ignore")]
        public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
        {
            var sql = loader.Load(mi);
            var tokenizer = new SqlTokenizer(sql);
            var tokens = tokenizer.Tokenize();
            var builder = new NodeBuilder(tokens);
            return builder.Build();
        }
    }
}
