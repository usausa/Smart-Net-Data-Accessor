namespace Smart.Data.Accessor.AotTests;

using System.Collections.Generic;

using Smart.Data.Accessor.Attributes;

// Single-source Pattern B accessor exercised through all three DI paths.
[DataAccessor]
internal sealed partial class AotAccessor
{
    [Query]
    public partial IReadOnlyList<AotData> QueryAll();
}
