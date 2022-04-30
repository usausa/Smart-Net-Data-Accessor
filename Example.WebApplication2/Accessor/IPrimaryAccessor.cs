namespace Example.WebApplication2.Accessor;

using Example.WebApplication2.Models;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
[Provider(DataSource.Primary)]
public interface IPrimaryAccessor
{
    [ExecuteScalar]
    ValueTask<int> CountDataAsync();

    [Query]
    ValueTask<List<DataEntity>> QueryDataAsync(string? type);
}
