namespace Example.WebApplication.Accessor;

using Example.WebApplication.Models;

using Smart.Data.Accessor.Attributes;

// Pattern B accessor: the connection comes from the injected IDbProvider (single source).
[DataAccessor]
internal sealed partial class WebDataAccessor
{
    [Query]
    public partial IReadOnlyList<WebData> QueryAll();
}
