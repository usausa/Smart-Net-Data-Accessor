namespace Smart.Data.Accessor.Generator.Visitors;

using Smart.Data.Accessor.Nodes;

internal sealed class UsingResolveVisitor : NodeVisitorBase
{
    private readonly HashSet<string> usings = [];

    private readonly HashSet<string> helpers = [];

    public IEnumerable<string> Usings => usings.OrderBy(static x => x);

    public IEnumerable<string> Helpers => helpers.OrderBy(static x => x);

    public override void Visit(UsingNode node)
    {
        if (node.IsStatic)
        {
            helpers.Add(node.Name);
        }
        else
        {
            usings.Add(node.Name);
        }
    }
}
