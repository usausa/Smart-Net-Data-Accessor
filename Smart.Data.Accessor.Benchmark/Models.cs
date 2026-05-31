namespace Smart.Data.Accessor.Benchmark;

using System.Diagnostics.CodeAnalysis;

using Smart.Data.Accessor.Attributes;

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
    [NotNullColumn]
    public long Id { get; set; }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet result types must be public for benchmark methods to expose them.")]
public sealed class BenchWideRow
{
    [NotNullColumn]
    public long Id { get; set; }

    [NotNullColumn]
    public string Name { get; set; } = string.Empty;

    [NotNullColumn]
    public int Age { get; set; }

    [NotNullColumn]
    public double Score { get; set; }

    [NotNullColumn]
    public bool Active { get; set; }

    [NotNullColumn]
    public int Status { get; set; }

    [NotNullColumn]
    public string Description { get; set; } = string.Empty;

    [NotNullColumn]
    public int Category { get; set; }

    [NotNullColumn]
    public string Tag { get; set; } = string.Empty;

    [NotNullColumn]
    public double Weight { get; set; }
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet result types must be public for benchmark methods to expose them.")]
public sealed class BenchEnumRow
{
    [NotNullColumn]
    public long Id { get; set; }

    [NotNullColumn]
    public string Name { get; set; } = string.Empty;

    [NotNullColumn]
    public BenchStatus Status { get; set; }
}
