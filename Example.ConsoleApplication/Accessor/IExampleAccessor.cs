namespace Example.ConsoleApplication.Accessor;

using Example.ConsoleApplication.Models;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Builders;

[DataAccessor]
public interface IExampleAccessor
{
    [Execute]
    void Create();

    [Insert]
    void Insert(DataEntity entity);

    [Query]
    List<DataEntity> QueryDataList(string? type = null, string? order = null);
}
