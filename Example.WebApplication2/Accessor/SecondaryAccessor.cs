namespace Example.WebApplication2.Accessor;

using Example.WebApplication2.Models;

using Smart.Data.Accessor.Attributes;

// [Provider("Secondary")] → reads the connection from providerSelector.GetProvider("Secondary").
[DataAccessor]
[Provider(DataSource.Secondary)]
internal sealed partial class SecondaryAccessor
{
    [Query]
    public partial IReadOnlyList<WebData> QueryAll();
}
