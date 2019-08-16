namespace Smart.Data.Accessor.Engine
{
    using System.Collections.Generic;
    using System.Data;
    using System.Data.Common;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator;
    using Smart.Mock;

    using Xunit;

    public class QueryTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IQuerySimpleDao
        {
            [Query]
            IList<DataEntity> QueryBufferd();

            [Query]
            IEnumerable<DataEntity> QueryNonBufferd();
        }

        [Fact]
        public void TestQueryBufferdSimple()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQuerySimpleDao>();

                var list = dao.QueryBufferd();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [Fact]
        public void TestQueryNonBufferdSimple()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQuerySimpleDao>();

                var list = dao.QueryNonBufferd().ToList();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [DataAccessor]
        public interface IQuerySimpleAsyncDao
        {
            [Query]
            Task<IList<DataEntity>> QueryBufferdAsync();

            [Query]
            Task<IEnumerable<DataEntity>> QueryNonBufferdAsync();
        }

        [Fact]
        public async Task TestQueryBufferdSimpleAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQuerySimpleAsyncDao>();

                var list = await dao.QueryBufferdAsync();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [Fact]
        public async Task TestQueryNonBufferdSimpleAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQuerySimpleAsyncDao>();

                var list = (await dao.QueryNonBufferdAsync()).ToList();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        //--------------------------------------------------------------------------------
        // With Connection
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IQueryWithConnectionDao
        {
            [Query]
            IList<DataEntity> QueryBufferd(DbConnection con);

            [Query]
            IEnumerable<DataEntity> QueryNonBufferd(DbConnection con);
        }

        [Fact]
        public void TestQueryBufferdWithConnection()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQueryWithConnectionDao>();

                con.Open();

                var list = dao.QueryBufferd(con);

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [Fact]
        public void TestQueryNonBufferdWithConnection()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQueryWithConnectionDao>();

                con.Open();

                var list = dao.QueryNonBufferd(con).ToList();

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [DataAccessor]
        public interface IQueryWithConnectionAsyncDao
        {
            [Query]
            Task<IList<DataEntity>> QueryBufferdAsync(DbConnection con);

            [Query]
            Task<IEnumerable<DataEntity>> QueryNonBufferdAsync(DbConnection con);
        }

        [Fact]
        public async Task TestQueryBufferdWithConnectionAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQueryWithConnectionAsyncDao>();

                con.Open();

                var list = await dao.QueryBufferdAsync(con);

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [Fact]
        public async Task TestQueryNonBufferdWithConnectionAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQueryWithConnectionAsyncDao>();

                con.Open();

                var list = (await dao.QueryNonBufferdAsync(con)).ToList();

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        //--------------------------------------------------------------------------------
        // Cancel
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IQueryCancelAsyncDao
        {
            [Query]
            Task<IList<DataEntity>> QueryBufferdAsync(CancellationToken cancel);

            [Query]
            Task<IEnumerable<DataEntity>> QueryNonBufferdAsync(CancellationToken cancel);
        }

        [Fact]
        public async Task TestQueryBufferdCancelAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQueryCancelAsyncDao>();

                var list = await dao.QueryBufferdAsync(default);

                Assert.Equal(2, list.Count);

                var cancel = new CancellationToken(true);
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await dao.QueryBufferdAsync(cancel));
            }
        }

        [Fact]
        public async Task TestQueryNonBufferdCancelAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQueryCancelAsyncDao>();

                var list = (await dao.QueryNonBufferdAsync(default)).ToList();

                Assert.Equal(2, list.Count);

                var cancel = new CancellationToken(true);
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await dao.QueryNonBufferdAsync(cancel));
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IQueryInvalidDao
        {
            [Query]
            void Query();
        }

        [Fact]
        public void TestQueryInvalid()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IQueryInvalidDao>());
        }

        [DataAccessor]
        public interface IQueryInvalidAsyncDao
        {
            [Query]
            Task QueryAsync();
        }

        [Fact]
        public void TestQueryInvalidAsync()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IQueryInvalidAsyncDao>());
        }
    }
}
