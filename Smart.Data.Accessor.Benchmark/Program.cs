namespace Smart.Data.Accessor.Benchmark
{
    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    using Smart.Mock.Data;

    public static class Program
    {
        public static void Main()
        {
            BenchmarkRunner.Run<DaoBenchmark>();
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.Default, MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.LongRun);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Ignore")]
    [Config(typeof(BenchmarkConfig))]
    public class DaoBenchmark
    {
        private MockDbConnection mockExecute;

        [IterationSetup]
        public void IterationSetup()
        {
            mockExecute = new MockDbConnection();
            mockExecute.SetupCommand(cmd => cmd.SetupResult(1));
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            mockExecute.Dispose();
        }

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        // TODO
    }
}
