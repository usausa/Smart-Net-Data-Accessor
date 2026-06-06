#pragma warning disable CA1812
namespace Smart.Data.Accessor.Tests.Models;

using System.Diagnostics.CodeAnalysis;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Converters;

// Stores a DateTime as Int64 ticks in the DB. The reader side calls FromDb to map the column
// (long) back to the CLR property (DateTime); the write side would call ToDb.
internal sealed class TicksConverter : IValueConverter<long, DateTime>
{
    public static DateTime FromDb(long dbValue) => new(dbValue, DateTimeKind.Utc);

    public static long ToDb(DateTime clrValue) => clrValue.Ticks;
}

internal sealed class TimestampEntity
{
    public long Id { get; set; }

    [TypeHandler(typeof(TicksConverter))]
    public DateTime CreatedAt { get; set; }
}

// No per-property [TypeHandler]: the converter is supplied at method / class / profile scope
// by the accessor that maps this entity.
internal sealed class PlainTimestampEntity
{
    public long Id { get; set; }

    public DateTime CreatedAt { get; set; }
}
