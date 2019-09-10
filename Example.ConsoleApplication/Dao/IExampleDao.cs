namespace Example.ConsoleApplication.Dao
{
    using System.Collections.Generic;

    using Example.ConsoleApplication.Models;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Attributes.Builders;

    [DataAccessor]
    public interface IExampleDao
    {
        [Execute]
        void Create();

        [Insert]
        void Insert(DataEntity entity);

        [Query]
        List<DataEntity> QueryDataList(string type = null);
    }
}
