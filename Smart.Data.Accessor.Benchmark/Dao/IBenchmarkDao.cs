namespace Smart.Data.Accessor.Benchmark.Dao
{
    using Smart.Data.Accessor.Attributes;

    [Dao]
    public interface IBenchmarkDao
    {
        [Execute]
        int ExecuteSimple();
    }
}
