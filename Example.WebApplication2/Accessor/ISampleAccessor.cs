namespace Example.WebApplication2.Accessor
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Example.WebApplication2.Models;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Attributes.Builders;

    [DataAccessor]
    public interface ISampleAccessor
    {
        [ExecuteScalar]
        Task<int> CountDataAsync();

        [Query]
        Task<IList<DataEntity>> QueryDataAsync(string type);

        [Insert]
        Task<int> InsertData(DataEntity entity);
    }
}
