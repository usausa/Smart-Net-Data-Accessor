#pragma warning disable CA1812
namespace Smart.Data.Accessor.Benchmark;

using System.Runtime.CompilerServices;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Converters;

public enum BenchStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2,
    Archived = 3
}

public sealed class BenchIntRow
{
    [NotNullColumn]
    public long Id { get; set; }
}

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

public sealed class BenchEnumRow
{
    [NotNullColumn]
    public long Id { get; set; }

    [NotNullColumn]
    public string Name { get; set; } = string.Empty;

    [NotNullColumn]
    public BenchStatus Status { get; set; }
}

// 10-column record (positional / constructor-based) for the mapping-strategy PoC benchmark.
public sealed record BenchWideRecord(
    long Id,
    string Name,
    int Age,
    double Score,
    bool Active,
    int Status,
    string Description,
    int Category,
    string Tag,
    double Weight);

// GetValue<T> fallback: uint has no DbDataReader typed getter, so reads go through
// ExecuteHelper.GetValue<uint> (per-row type coercion when the column arrives as long).
public sealed class BenchUIntRow
{
    [NotNullColumn]
    public long Id { get; set; }

    [NotNullColumn]
    public uint Value { get; set; }
}

// Case 4: a DateTime stored as Int64 ticks, mapped on read via [TypeHandler<>].
internal sealed class BenchTicksConverter : IValueConverter<long, DateTime>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime FromDb(long dbValue) => new(dbValue, DateTimeKind.Utc);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToDb(DateTime clrValue) => clrValue.Ticks;
}

public sealed class BenchTicksRow
{
    [NotNullColumn]
    public long Id { get; set; }

    [NotNullColumn]
    [TypeHandler(typeof(BenchTicksConverter))]
    public DateTime Created { get; set; }
}
