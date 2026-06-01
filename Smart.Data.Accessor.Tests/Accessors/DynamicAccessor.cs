namespace Smart.Data.Accessor.Tests.Accessors;

using System.Collections.Generic;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class DynamicAccessor
{
    [Query]
    public partial IReadOnlyList<DataEntity> QueryByOptionalId(DbConnection con, int? id);

    [Query]
    public partial IReadOnlyList<DataEntity> QueryByIds(DbConnection con, IEnumerable<long> ids);
}
