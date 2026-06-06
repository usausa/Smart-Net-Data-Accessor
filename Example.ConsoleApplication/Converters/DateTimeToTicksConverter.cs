namespace Example.ConsoleApplication.Converters;

using System.Runtime.CompilerServices;

using Smart.Data.Accessor.Converters;

#pragma warning disable CA1812
internal sealed class DateTimeToTicksConverter : IValueConverter<long, DateTime>
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime FromDb(long dbValue) => new(dbValue, DateTimeKind.Utc);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToDb(DateTime clrValue) => clrValue.Ticks;
}
#pragma warning restore CA1812
