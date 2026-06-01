namespace Smart.Data.Accessor.Tests.Accessors;

using System.Collections.Generic;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Tests.Models;

// Pattern B: no DbConnection/DbTransaction argument → connection comes from the
// injected IDbProvider (Usa.Smart.Data).
[DataAccessor]
internal sealed partial class ProviderAccessor
{
    [Query]
    public partial IReadOnlyList<DataEntity> QueryAll();
}
