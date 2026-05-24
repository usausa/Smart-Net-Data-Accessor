namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Smart.Data.Accessor.Generator;
using Smart.Data.Accessor.Nodes;
using Smart.Data.Accessor.Tokenizer;

public abstract class LoaderMethodAttribute : MethodAttribute
{
    protected LoaderMethodAttribute(MethodType methodType)
        : base(CommandType.Text, methodType)
    {
    }

    [RequiresUnreferencedCode("Required to match base class MethodAttribute.GetNodes contract.")]
    public override IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi)
    {
        var sql = loader.Load(mi);
        var tokenizer = new SqlTokenizer(sql);
        var tokens = SqlTokenNormalizer.Normalize(tokenizer.Tokenize());
        var builder = new NodeBuilder(tokens);
        return builder.Build();
    }
}
