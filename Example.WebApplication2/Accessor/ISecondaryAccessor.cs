namespace Example.WebApplication2.Accessor;

using Example.WebApplication2.Models;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
[Provider(DataSource.Secondary)]
public interface ISecondaryAccessor
{
    [ExecuteScalar]
    ValueTask<int> CountDataAsync();

    [Query]
    ValueTask<IList<DataEntity>> QueryDataAsync(string? type);
}
