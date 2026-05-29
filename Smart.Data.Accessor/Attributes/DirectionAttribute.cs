namespace Smart.Data.Accessor.Attributes;

using System;
using System.Data;

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class DirectionAttribute : Attribute
{
    public ParameterDirection Direction { get; }

    public DirectionAttribute(ParameterDirection direction)
    {
        Direction = direction;
    }
}
