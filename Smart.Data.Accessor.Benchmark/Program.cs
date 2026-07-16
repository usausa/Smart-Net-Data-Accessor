namespace Smart.Data.Accessor.Benchmark;

using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        // アセンブリ内の全ベンチマークを自動発見する(手書きの型リストは追加漏れで --filter から
        // 静かに漏れるため使わない)。
        // Auto-discover every benchmark in the assembly (a hand-written type list silently drops
        // newly added benchmarks from --filter when someone forgets to append).
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
