namespace Smart.Data.Accessor.Tests.Accessors;

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

    // /*# sort */ : the argument value is substituted into the SQL text (dynamic ORDER BY), not bound.
    [Query]
    public partial IReadOnlyList<DataEntity> QueryOrderBy(DbConnection con, string sort);
}
