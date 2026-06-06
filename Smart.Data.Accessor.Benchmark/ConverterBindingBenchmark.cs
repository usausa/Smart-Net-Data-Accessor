namespace Smart.Data.Accessor.Benchmark;

using System.Data.Common;
using System.Runtime.CompilerServices;

using BenchmarkDotNet.Attributes;

using Smart.Data.Accessor.Converters;
using Smart.Data.Accessor.Helpers;
using Smart.Mock.Data;

// 改善2 (P6 gate + P8 post-impl): benchmark for the converter-sharing overload
// ExecuteHelper.AddInParameter<TConverter, TDb, TClr> (calls static abstract TConverter.ToDb through a
// generic constraint) vs the current generator-emitted direct call Conv.ToDb(x).
//
//  * Micro*  isolates the ToDb call path (long accumulate, no boxing) — P6's adoption gate. The concern
//            was that TConverter being a sealed reference type would force shared generics (indirect,
//            non-inlined static-abstract call); P6 confirmed the JIT devirtualises + inlines it (Code
//            Size identical, disassembly diff empty).
//  *Pipeline* exercises the *shipped* overload through a real DbCommand (P8 post-implementation
//            non-regression): inline ToDb + AddInParameter(object?) vs AddInParameter<TConverter,TDb,TClr>.
//
// Run: dotnet run -c Release --project Smart.Data.Accessor.Benchmark -- --filter *ConverterBinding*
#pragma warning disable CA1001
[Config(typeof(BenchmarkConfig))]
public class ConverterBindingBenchmark
{
    private const int Ops = 1024;

    private DateTime seed;
    private MockRepeatDbConnection mock = default!;
    private DbCommand cmd = default!;

    [GlobalSetup]
    public void Setup()
    {
        seed = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#pragma warning disable CA2000
        mock = new MockRepeatDbConnection(new MockDataReader([new MockColumn(typeof(long), "X")], []));
#pragma warning restore CA2000
        cmd = mock.CreateCommand();
    }

    [GlobalCleanup]
    public void Cleanup() => mock.Dispose();

    // Current generator output: a direct static call to the concrete converter.
    [Benchmark(Baseline = true, OperationsPerInvoke = Ops, Description = "Micro: Direct Conv.ToDb (current emit)")]
    public long MicroDirect()
    {
        long acc = 0;
        var v = seed;
        for (var i = 0; i < Ops; i++)
        {
            acc += BenchTicksConverter.ToDb(v);
            v = v.AddTicks(1);
        }
        return acc;
    }

    // Shared-overload core: the static abstract ToDb reached through a generic type constraint.
    [Benchmark(OperationsPerInvoke = Ops, Description = "Micro: Generic <TConv> ToDb (overload core)")]
    public long MicroGeneric()
    {
        long acc = 0;
        var v = seed;
        for (var i = 0; i < Ops; i++)
        {
            acc += ToDb<BenchTicksConverter, long, DateTime>(v);
            v = v.AddTicks(1);
        }
        return acc;
    }

    // Current emit shape: gen-time inline ToDb boxed into the non-generic AddInParameter(object?).
    [Benchmark(OperationsPerInvoke = Ops, Description = "Pipeline: inline ToDb + AddInParameter (current emit)")]
    public void PipelineInline()
    {
        for (var i = 0; i < Ops; i++)
        {
            cmd.Parameters.Clear();
            ExecuteHelper.AddInParameter(cmd, "@p", (object?)BenchTicksConverter.ToDb(seed));
        }
    }

    // Shipped P8 overload: the generator emits this and the helper calls ToDb + handles null/DBNull.
    [Benchmark(OperationsPerInvoke = Ops, Description = "Pipeline: AddInParameter<TConv,TDb,TClr> (P8 overload)")]
    public void PipelineOverload()
    {
        for (var i = 0; i < Ops; i++)
        {
            cmd.Parameters.Clear();
            ExecuteHelper.AddInParameter<BenchTicksConverter, long, DateTime>(cmd, "@p", seed);
        }
    }

    // The micro shared-overload kernel (mirrors ExecuteHelper.AddInParameter<…>'s ToDb call).
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TDb ToDb<TConverter, TDb, TClr>(TClr value)
        where TConverter : IValueConverter<TDb, TClr>
        => TConverter.ToDb(value);
}
#pragma warning restore CA1001
