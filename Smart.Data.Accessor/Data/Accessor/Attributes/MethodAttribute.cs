namespace Smart.Data.Accessor.Attributes;

using System;
using System.Collections.Generic;
using System.Data;
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

    public abstract IReadOnlyList<INode> GetNodes(ISqlLoader loader, MethodInfo mi);
}
