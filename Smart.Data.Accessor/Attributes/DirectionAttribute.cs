namespace Smart.Data.Accessor.Attributes;

using System;
using System.Data;

// Parameter: out/ref OUT params (sync). Property: POCO-argument output properties for
// [Procedure]/[DirectSql] OUT aggregation (spec §5.6).
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public sealed class DirectionAttribute : Attribute
{
    public ParameterDirection Direction { get; }

    public DirectionAttribute(ParameterDirection direction)
    {
        Direction = direction;
    }
}
