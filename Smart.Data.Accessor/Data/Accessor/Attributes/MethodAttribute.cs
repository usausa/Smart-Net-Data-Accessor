namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Smart.Data.Accessor.Generator;
using Smart.Data.Accessor.Nodes;

[AttributeUsage(AttributeTargets.Method)]
public abstract class MethodAttribute : Attribute
{
    public CommandType CommandType { get; }

    public MethodType MethodType { get; }

    protected MethodAttribute(CommandType commandType, MethodType methodType)
    {
        CommandType = commandType;
        MethodType = methodType;
    }

    [RequiresUnreferencedCode("GetNodes uses reflection via BuildHelper and ConfigHelper and may not work with trimming.")]
    public abstract IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi);
}
