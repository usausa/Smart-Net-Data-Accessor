namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

// POCO argument exercising all directions (no converter): A/B input, Sum output, Acc InputOutput.
internal sealed class CalcArgs
{
    public int A { get; set; }

    public int B { get; set; }

    [Direction(ParameterDirection.Output)]
    public int Sum { get; set; }

    [Direction(ParameterDirection.InputOutput)]
    public int Acc { get; set; }
}

// POCO argument with a [TypeHandler] (DateTime <-> Int64 ticks) across all directions.
internal sealed class ConvArgs
{
    [TypeHandler(typeof(TicksConverter))]
    public DateTime In { get; set; }

    [Direction(ParameterDirection.Output)]
    [TypeHandler(typeof(TicksConverter))]
    public DateTime Out { get; set; }

    [Direction(ParameterDirection.InputOutput)]
    [TypeHandler(typeof(TicksConverter))]
    public DateTime Stamp { get; set; }
}

[DataAccessor]
internal sealed partial class ProcMockAccessor
{
    // Output + InputOutput + scalar RETURN value (Pattern A: DbConnection arg for Mock setup).
    [Procedure("usp_Calc")]
    [ExecuteScalar]
    public partial int Calc(DbConnection con, CalcArgs args);

    // Converter across input / output / InputOutput.
    [Procedure("usp_Conv")]
    [Execute]
    public partial void Conv(DbConnection con, ConvArgs args);
}
