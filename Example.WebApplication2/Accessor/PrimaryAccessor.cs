namespace Example.WebApplication2.Accessor;

using Example.WebApplication2.Models;

using Smart.Data.Accessor.Attributes;

// [Provider("Primary")] → the generated ctor takes IDbProviderSelector and reads the connection
// from providerSelector.GetProvider("Primary") (multi-source Pattern B).
[DataAccessor]
[Provider(DataSource.Primary)]
internal sealed partial class PrimaryAccessor
{
    [Query]
    public partial IReadOnlyList<WebData> QueryAll();
}
