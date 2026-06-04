namespace Example.ConsoleApplication.Models;

using Example.ConsoleApplication.Converters;

using Smart.Data.Accessor.Attributes;

// Demonstrates a custom [TypeHandler<>]: OccurredAt is stored as Int64 ticks in the DB.
// The write side calls DateTimeToTicksConverter.ToDb (DateTime -> long) and the read side calls
// FromDb (long -> UTC DateTime), so the CLR property stays a DateTime end to end.
internal sealed class EventEntity
{
    [Key]
    [DatabaseManaged]
    public long Id { get; set; }

    [TypeHandler(typeof(DateTimeToTicksConverter))]
    public DateTime OccurredAt { get; set; }
}
