namespace Smart.Data.Accessor.Benchmark;

using System.Diagnostics.CodeAnalysis;

using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromTypes([typeof(QueryBenchmark), typeof(ConverterBindingBenchmark)]).Run(args);
    }
}
