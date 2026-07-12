namespace Smart.Data.Accessor.Benchmark;

using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromTypes([typeof(QueryBenchmark), typeof(ConverterBindingBenchmark), typeof(ExecuteHelperBenchmark), typeof(MappingStrategyBenchmark)]).Run(args);
    }
}
