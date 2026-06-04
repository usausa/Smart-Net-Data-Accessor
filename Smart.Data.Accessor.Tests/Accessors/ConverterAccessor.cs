namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

// Reader-side [TypeHandler<>]: the CreatedAt column (stored as Int64 ticks) is mapped back to
// DateTime via TicksConverter.FromDb.
[DataAccessor]
internal sealed partial class ConverterAccessor
{
    [Query]
    public partial IReadOnlyList<TimestampEntity> QueryAll(DbConnection con);
}
