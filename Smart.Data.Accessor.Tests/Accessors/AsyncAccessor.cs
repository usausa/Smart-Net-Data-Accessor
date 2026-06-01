namespace Smart.Data.Accessor.Tests.Accessors;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

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
