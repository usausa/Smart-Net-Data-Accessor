namespace Smart.Data.Accessor.Engine
{
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class ExecuteScalarTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IExecuteScalarSimpleDao
        {
            [ExecuteScalar]
            long ExecuteScalar();
        }

        [Fact]
        public void ExecuteScalarSimple()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarSimpleDao>();

                var count = dao.ExecuteScalar();

                Assert.Equal(2, count);
            }
        }

        [Dao]
        public interface IExecuteScalarSimpleAsyncDao
        {
            [ExecuteScalar]
            Task<long> ExecuteScalarAsync();
        }

        [Fact]
        public async Task ExecuteScalarSimpleAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarSimpleAsyncDao>();

                var count = await dao.ExecuteScalarAsync();

                Assert.Equal(2, count);
            }
        }

        // TODO use sqlite, with con, without con
        // TODO ref mapper
    }
}
