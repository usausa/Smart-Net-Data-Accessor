namespace Example.WebApplication.Dao
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Example.WebApplication.Models;

    using Smart.Data.Accessor.Attributes;

    [Dao]
    public interface ISampleDao
    {
        [ExecuteScalar]
        Task<int> CountDataAsync();

        [Query]
        Task<IList<DataEntity>> QueryDataAsync(string type);
    }
}
