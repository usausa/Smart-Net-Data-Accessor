namespace Smart.Data.Accessor.Benchmark;

using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Running;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "Entry point for the benchmark executable.")]
public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<QueryBenchmark>(args: args);
    }
}
