namespace Example.WebApplication2.Accessor
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Example.WebApplication2.Models;

    using Smart.Data.Accessor.Attributes;

    [DataAccessor]
    [Provider(DataSource.Secondary)]
    public interface ISecondaryAccessor
    {
        [ExecuteScalar]
        Task<int> CountDataAsync();

        [Query]
        Task<IList<DataEntity>> QueryDataAsync(string type);
    }
}
