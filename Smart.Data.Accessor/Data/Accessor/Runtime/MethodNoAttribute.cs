namespace Smart.Data.Accessor.Runtime;

using System;

[AttributeUsage(AttributeTargets.Method)]
public sealed class MethodNoAttribute : Attribute
{
    public int No { get; }

    public MethodNoAttribute(int no)
    {
        No = no;
    }
}
