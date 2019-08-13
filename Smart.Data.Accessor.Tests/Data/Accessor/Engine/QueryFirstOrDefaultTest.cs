namespace Smart.Data.Accessor.Engine
{
    using System.Data;
    using System.Data.Common;
    using System.Threading;
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator;
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
        public void TestQueryFirstOrDefaultSimple()
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
        public async Task TestQueryFirstOrDefaultSimpleAsync()
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

        //--------------------------------------------------------------------------------
        // With Connection
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IQueryFirstOrDefaultWithConnectionDao
        {
            [QueryFirstOrDefault]
            DataEntity QueryFirstOrDefault(DbConnection con, long id);
        }

        [Fact]
        public void TestQueryFirstOrDefaultWithConnection()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
                    .Build();
                var dao = generator.Create<IQueryFirstOrDefaultWithConnectionDao>();

                con.Open();

                var entity = dao.QueryFirstOrDefault(con, 1L);

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.NotNull(entity);
                Assert.Equal(1, entity.Id);
                Assert.Equal("Data-1", entity.Name);

                entity = dao.QueryFirstOrDefault(con, 2L);
                Assert.Null(entity);
            }
        }

        [Dao]
        public interface IQueryFirstOrDefaultWithConnectionAsyncDao
        {
            [QueryFirstOrDefault]
            Task<DataEntity> QueryFirstOrDefaultAsync(DbConnection con, long id);
        }

        [Fact]
        public async Task TestQueryFirstOrDefaultWithConnectionAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
                    .Build();
                var dao = generator.Create<IQueryFirstOrDefaultWithConnectionAsyncDao>();

                con.Open();

                var entity = await dao.QueryFirstOrDefaultAsync(con, 1L);

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.NotNull(entity);
                Assert.Equal(1, entity.Id);
                Assert.Equal("Data-1", entity.Name);

                entity = await dao.QueryFirstOrDefaultAsync(con, 2L);
                Assert.Null(entity);
            }
        }

        //--------------------------------------------------------------------------------
        // Cancel
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IQueryFirstOrDefaultCancelAsyncDao
        {
            [QueryFirstOrDefault]
            Task<DataEntity> QueryFirstOrDefaultAsync(long id, CancellationToken cancel);
        }

        [Fact]
        public async Task TestQueryFirstOrDefaultCancelAsync()
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
                var dao = generator.Create<IQueryFirstOrDefaultCancelAsyncDao>();

                var entity = await dao.QueryFirstOrDefaultAsync(1L, default);

                Assert.NotNull(entity);

                var cancel = new CancellationToken(true);
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await dao.QueryFirstOrDefaultAsync(1L, cancel));
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IQueryFirstOrDefaultInvalidDao
        {
            [QueryFirstOrDefault]
            void QueryFirstOrDefault();
        }

        [Fact]
        public void TestQueryFirstOrDefaultInvalid()
        {
            var generator = new GeneratorBuilder()
                .EnableDebug()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IQueryFirstOrDefaultInvalidDao>());
        }

        [Dao]
        public interface IQueryFirstOrDefaultInvalidAsyncDao
        {
            [QueryFirstOrDefault]
            Task QueryFirstOrDefaultAsync();
        }

        [Fact]
        public void TestQueryFirstOrDefaultInvalidAsync()
        {
            var generator = new GeneratorBuilder()
                .EnableDebug()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IQueryFirstOrDefaultInvalidAsyncDao>());
        }
    }
}
