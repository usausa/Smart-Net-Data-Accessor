namespace Smart.Data.Accessor.Attributes;

using System;
using System.Data;

[AttributeUsage(AttributeTargets.Property)]
public abstract class DirectionAttribute : Attribute
{
    public ParameterDirection Direction { get; }

    protected DirectionAttribute(ParameterDirection direction)
    {
        Direction = direction;
    }
}

public sealed class InputAttribute : DirectionAttribute
{
    public InputAttribute()
        : base(ParameterDirection.Input)
    {
    }
}

public sealed class InputOutputAttribute : DirectionAttribute
{
    public InputOutputAttribute()
        : base(ParameterDirection.InputOutput)
    {
    }
}

public sealed class OutputAttribute : DirectionAttribute
{
    public OutputAttribute()
        : base(ParameterDirection.Output)
    {
    }
}

public sealed class ReturnValueAttribute : DirectionAttribute
{
    public ReturnValueAttribute()
        : base(ParameterDirection.ReturnValue)
    {
    }
}
