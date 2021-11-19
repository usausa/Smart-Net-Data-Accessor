namespace Example.WebApplication.Accessor;

using System.Collections.Generic;
using System.Threading.Tasks;

using Example.WebApplication.Models;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders;

[DataAccessor]
public interface ISampleAccessor
{
    [ExecuteScalar]
    ValueTask<int> CountDataAsync();

    [Query]
    ValueTask<IList<DataEntity>> QueryDataAsync(string? type);

    [Insert]
    ValueTask<int> InsertData(DataEntity entity);
}
