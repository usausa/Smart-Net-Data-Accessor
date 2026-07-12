namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class MappingAccessor
{
    [Query]
    public partial IReadOnlyList<MappingEntity> QueryEntities(DbConnection con);

    [Query]
    public partial IReadOnlyList<MappingRecord> QueryRecords(DbConnection con);
}
