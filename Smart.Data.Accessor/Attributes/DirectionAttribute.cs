namespace Smart.Data.Accessor.Attributes;

using System.Data;
using System.Diagnostics.CodeAnalysis;

// Parameter: out/ref OUT params (sync). Property: POCO-argument output properties for
// [Procedure]/[DirectSql] OUT aggregation (spec §5.6).
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
[ExcludeFromCodeCoverage]
public sealed class DirectionAttribute : Attribute
{
    public ParameterDirection Direction { get; }

    public DirectionAttribute(ParameterDirection direction)
    {
        Direction = direction;
    }
}
