namespace Smart.Data.Accessor.Tests.Accessors;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

[DataAccessor]
internal sealed partial class AsyncAccessor
{
    [Query]
    public partial Task<IReadOnlyList<DataEntity>> QueryAllAsync(DbConnection con, CancellationToken cancel);

    [Execute]
    public partial Task<int> InsertAsync(DbConnection con, string name, int type);
}
