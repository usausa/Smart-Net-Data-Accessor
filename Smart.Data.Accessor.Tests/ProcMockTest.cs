namespace Smart.Data.Accessor.Tests;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Mock.Data;

using Xunit;

// Stored-procedure POCO parameter aggregation verified against the Mock —
// input/output/InputOutput directions, scalar RETURN value mapping, and [TypeHandler] conversion
// (ToDb on input, FromDb on output) across all directions. The Executing hook simulates the proc by
// setting OUT parameter values; the generated write-back reads them back into the POCO.
public sealed class ProcMockTest
{
    [Fact]
    public void DirectionsAndReturnValue()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                MockDbParameter P(string n)
                {
                    foreach (MockDbParameter p in c.Parameters)
                    {
                        if (p.ParameterName == n)
                        {
                            return p;
                        }
                    }
                    throw new InvalidOperationException("parameter not found: " + n);
                }

                Assert.Equal(7, (int)P("@A").Value!);
                Assert.Equal(3, (int)P("@B").Value!);
                Assert.Equal(100, (int)P("@Acc").Value!);   // InputOutput input value

                // Simulate the procedure writing the OUT / InputOutput / RETURN values.
                P("@Sum").Value = 10;
                P("@Acc").Value = 105;
                P("@__ReturnValue").Value = 21;
            };
            cmd.SetupResult(0);
        });

        var args = new CalcArgs { A = 7, B = 3, Acc = 100 };
        var ret = new ProcMockAccessor().Calc(con, args);

        Assert.Equal(21, ret);        // RETURN value → method return
        Assert.Equal(10, args.Sum);   // Output → write-back
        Assert.Equal(105, args.Acc);  // InputOutput → write-back
    }

    [Fact]
    public void ConverterAcrossDirections()
    {
        var inDate = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var stampIn = new DateTime(2020, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var outTicks = new DateTime(2030, 12, 31, 0, 0, 0, DateTimeKind.Utc).Ticks;
        var stampOutTicks = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;

        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                MockDbParameter P(string n)
                {
                    foreach (MockDbParameter p in c.Parameters)
                    {
                        if (p.ParameterName == n)
                        {
                            return p;
                        }
                    }
                    throw new InvalidOperationException("parameter not found: " + n);
                }

                // Input / InputOutput: DateTime converted to Int64 ticks via TicksConverter.ToDb.
                Assert.Equal(inDate.Ticks, (long)P("@In").Value!);
                Assert.Equal(stampIn.Ticks, (long)P("@Stamp").Value!);

                // Simulate the proc returning ticks (long) on the OUT / InputOutput parameters.
                P("@Out").Value = outTicks;
                P("@Stamp").Value = stampOutTicks;
            };
            cmd.SetupResult(0);
        });

        var args = new ConvArgs { In = inDate, Stamp = stampIn };
        new ProcMockAccessor().Conv(con, args);

        // Output / InputOutput: Int64 ticks converted back to DateTime via TicksConverter.FromDb.
        Assert.Equal(new DateTime(outTicks, DateTimeKind.Utc), args.Out);
        Assert.Equal(new DateTime(stampOutTicks, DateTimeKind.Utc), args.Stamp);
    }
}
