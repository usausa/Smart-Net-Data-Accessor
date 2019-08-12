namespace Target.Dao
{
    using Smart.Data.Accessor.Attributes;

    using Target.Entity;

    [Dao]
    public interface ISampleDao
    {
        [QueryFirstOrDefault]
        DataEntity QueryData(long id);
    }
}
