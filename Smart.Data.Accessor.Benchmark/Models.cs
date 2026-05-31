namespace Smart.Data.Accessor.Benchmark;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet result types must be public for benchmark methods to expose them.")]
public enum BenchStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2,
    Archived = 3,
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet result types must be public for benchmark methods to expose them.")]
public sealed class BenchIntRow
{
    public long Id { get; set; }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet result types must be public for benchmark methods to expose them.")]
public sealed class BenchWideRow
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public double Score { get; set; }
    public bool Active { get; set; }
    public int Status { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Category { get; set; }
    public string Tag { get; set; } = string.Empty;
    public double Weight { get; set; }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet result types must be public for benchmark methods to expose them.")]
public sealed class BenchEnumRow
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public BenchStatus Status { get; set; }
}
