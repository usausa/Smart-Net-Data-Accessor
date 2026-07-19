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

// 序数解決の閾値(NarrowOrdinalGroupThreshold = 16)を超えるエンティティ。閾値以下の BenchWideRow が直比較連鎖で
// 解決されるのに対し、こちらはサンプリングハッシュ switch で解決される。マイクロベンチは __From をタイトループで
// 回すため構造的に直比較(1 本の大きなメソッド)へ有利に働くが、実運用の __From は 1 クエリ 1 回のコールド呼び出し。
// ここはその実運用形で測るための入口。
// An entity above the ordinal-resolution threshold (NarrowOrdinalGroupThreshold = 16). BenchWideRow stays on the
// direct-comparison chain; this one resolves through the sampling-hash switch. A microbenchmark spins __From in a
// tight loop, which structurally favours the direct form (one large method), whereas production calls __From once per
// query, cold - this is the entry point for measuring it that way.
public sealed class BenchExtraWideRow
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

    [NotNullColumn]
    public int Owner { get; set; }

    [NotNullColumn]
    public int Team { get; set; }

    [NotNullColumn]
    public int Level { get; set; }

    [NotNullColumn]
    public int Position { get; set; }

    [NotNullColumn]
    public int Version { get; set; }

    [NotNullColumn]
    public string City { get; set; } = string.Empty;

    [NotNullColumn]
    public string State { get; set; } = string.Empty;

    [NotNullColumn]
    public string Country { get; set; } = string.Empty;

    [NotNullColumn]
    public string Note { get; set; } = string.Empty;

    [NotNullColumn]
    public string Memo { get; set; } = string.Empty;
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

// 10-column record (positional / constructor-based) for the record-vs-class comparison.
// [property: NotNullColumn] mirrors BenchWideRow so the generated record path also skips IsDBNull,
// keeping the comparison apples-to-apples (isolates ctor vs object-initializer).
public sealed record BenchWideRecord(
    [property: NotNullColumn] long Id,
    [property: NotNullColumn] string Name,
    [property: NotNullColumn] int Age,
    [property: NotNullColumn] double Score,
    [property: NotNullColumn] bool Active,
    [property: NotNullColumn] int Status,
    [property: NotNullColumn] string Description,
    [property: NotNullColumn] int Category,
    [property: NotNullColumn] string Tag,
    [property: NotNullColumn] double Weight);

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
