namespace Smart.Data.Accessor.Tests.Accessors;

using System.Collections.Generic;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class QueryAccessor
{
    [Query]
    public partial IReadOnlyList<DataEntity> QueryAll(DbConnection con);

    [QueryFirst]
    public partial DataEntity? QueryFirst(DbConnection con);
}
