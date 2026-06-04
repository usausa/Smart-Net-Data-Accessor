namespace Smart.Data.Accessor.Benchmark;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Converters;

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

// Case 4: a DateTime stored as Int64 ticks, mapped on read via [TypeHandler<>] (spec §7.4 / §7.10).
[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Never instantiated; static abstract interface impl host (spec §7.4).")]
internal sealed class BenchTicksConverter : IValueConverter<long, DateTime>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime FromDb(long dbValue) => new(dbValue, DateTimeKind.Utc);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToDb(DateTime clrValue) => clrValue.Ticks;
}

[SuppressMessage("Design", "CA1515:Consider making public types internal", Justification = "BenchmarkDotNet result types must be public for benchmark methods to expose them.")]
public sealed class BenchTicksRow
{
    [NotNullColumn]
    public long Id { get; set; }

    [NotNullColumn]
    [TypeHandler(typeof(BenchTicksConverter))]
    public DateTime Created { get; set; }
}
