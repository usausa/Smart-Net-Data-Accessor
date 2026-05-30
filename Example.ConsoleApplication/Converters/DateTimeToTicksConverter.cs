namespace Example.ConsoleApplication.Converters;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using Smart.Data.Accessor.Converters;

[SuppressMessage("Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Never instantiated; static abstract interface impl host (spec §7.4).")]
internal sealed class DateTimeToTicksConverter : IValueConverter<long, DateTime>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime FromDb(long dbValue) => new(dbValue, DateTimeKind.Utc);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToDb(DateTime clrValue) => clrValue.Ticks;
}
