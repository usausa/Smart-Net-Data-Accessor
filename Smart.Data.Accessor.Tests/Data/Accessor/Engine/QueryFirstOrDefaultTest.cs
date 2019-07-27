namespace Smart.Data.Accessor.Engine
{
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class QueryFirstOrDefaultTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IQueryFirstOrDefaultSimpleDao
        {
            [QueryFirstOrDefault]
            DataEntity QueryFirstOrDefault(long id);
        }

        [Fact]
        public void QueryFirstOrDefaultSimple()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
                    .Build();
                var dao = generator.Create<IQueryFirstOrDefaultSimpleDao>();

                var entity = dao.QueryFirstOrDefault(1L);

                Assert.NotNull(entity);
                Assert.Equal(1, entity.Id);
                Assert.Equal("Data-1", entity.Name);

                entity = dao.QueryFirstOrDefault(2L);
                Assert.Null(entity);
            }
        }

        [Dao]
        public interface IQueryFirstOrDefaultSimpleAsyncDao
        {
            [QueryFirstOrDefault]
            Task<DataEntity> QueryFirstOrDefaultAsync(long id);
        }

        [Fact]
        public async Task QueryFirstOrDefaultSimpleAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
                    .Build();
                var dao = generator.Create<IQueryFirstOrDefaultSimpleAsyncDao>();

                var entity = await dao.QueryFirstOrDefaultAsync(1L);

                Assert.NotNull(entity);
                Assert.Equal(1, entity.Id);
                Assert.Equal("Data-1", entity.Name);

                entity = await dao.QueryFirstOrDefaultAsync(2L);
                Assert.Null(entity);
            }
        }

        // TODO use sqlite, with con, without con
        // TODO ref mapper
    }
}
