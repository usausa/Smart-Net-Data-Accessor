namespace Smart.Data.Accessor.Attributes;

using System;

// Suffix used to disambiguate overloaded accessor methods → SQL file name.
[AttributeUsage(AttributeTargets.Method)]
public sealed class MethodNoAttribute : Attribute
{
    public int No { get; }

    public MethodNoAttribute(int no)
    {
        No = no;
    }
}
