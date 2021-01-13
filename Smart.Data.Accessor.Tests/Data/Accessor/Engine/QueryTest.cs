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

        [Optimize(true)]
        [DataAccessor]
        public interface IQuerySimpleAccessor
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
                var accessor = generator.Create<IQuerySimpleAccessor>();

                var list = accessor.QueryBufferd();

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
                var accessor = generator.Create<IQuerySimpleAccessor>();

                var list = accessor.QueryNonBufferd().ToList();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [Optimize(true)]
        [DataAccessor]
        public interface IQuerySimpleAsyncAccessor
        {
            [Query]
            ValueTask<IList<DataEntity>> QueryBufferdAsync();

            [Query]
            IAsyncEnumerable<DataEntity> QueryNonBufferdAsync();
        }

        [Fact]
        public async ValueTask TestQueryBufferdSimpleAsync()
        {
            await using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var accessor = generator.Create<IQuerySimpleAsyncAccessor>();

                var list = await accessor.QueryBufferdAsync();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [Fact]
        public async ValueTask TestQueryNonBufferdSimpleAsync()
        {
            await using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var accessor = generator.Create<IQuerySimpleAsyncAccessor>();

                var list = await accessor.QueryNonBufferdAsync().ToListAsync();

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

        [Optimize(true)]
        [DataAccessor]
        public interface IQueryWithConnectionAccessor
        {
            [Query]
            IList<DataEntity> QueryBufferd(DbConnection con);

            [Query]
            IEnumerable<DataEntity> QueryNonBufferd(DbConnection con);
        }

        [Fact]
        public void TestQueryBufferdWithConnection()
        {
            using var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
            var generator = new TestFactoryBuilder()
                .SetSql("SELECT * FROM Data ORDER BY Id")
                .Build();
            var accessor = generator.Create<IQueryWithConnectionAccessor>();

            con.Open();

            var list = accessor.QueryBufferd(con);

            Assert.Equal(ConnectionState.Open, con.State);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].Id);
            Assert.Equal("Data-1", list[0].Name);
            Assert.Equal(2, list[1].Id);
            Assert.Equal("Data-2", list[1].Name);
        }

        [Fact]
        public void TestQueryNonBufferdWithConnection()
        {
            using var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
            var generator = new TestFactoryBuilder()
                .SetSql("SELECT * FROM Data ORDER BY Id")
                .Build();
            var accessor = generator.Create<IQueryWithConnectionAccessor>();

            con.Open();

            var list = accessor.QueryNonBufferd(con).ToList();

            Assert.Equal(ConnectionState.Open, con.State);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].Id);
            Assert.Equal("Data-1", list[0].Name);
            Assert.Equal(2, list[1].Id);
            Assert.Equal("Data-2", list[1].Name);
        }

        [Optimize(true)]
        [DataAccessor]
        public interface IQueryWithConnectionAsyncAccessor
        {
            [Query]
            ValueTask<IList<DataEntity>> QueryBufferdAsync(DbConnection con);

            [Query]
            IAsyncEnumerable<DataEntity> QueryNonBufferdAsync(DbConnection con);
        }

        [Fact]
        public async ValueTask TestQueryBufferdWithConnectionAsync()
        {
            await using var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
            var generator = new TestFactoryBuilder()
                .SetSql("SELECT * FROM Data ORDER BY Id")
                .Build();
            var accessor = generator.Create<IQueryWithConnectionAsyncAccessor>();

            await con.OpenAsync();

            var list = await accessor.QueryBufferdAsync(con);

            Assert.Equal(ConnectionState.Open, con.State);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].Id);
            Assert.Equal("Data-1", list[0].Name);
            Assert.Equal(2, list[1].Id);
            Assert.Equal("Data-2", list[1].Name);
        }

        [Fact]
        public async ValueTask TestQueryNonBufferdWithConnectionAsync()
        {
            await using var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
            var generator = new TestFactoryBuilder()
                .SetSql("SELECT * FROM Data ORDER BY Id")
                .Build();
            var accessor = generator.Create<IQueryWithConnectionAsyncAccessor>();

            await con.OpenAsync();

            var list = await accessor.QueryNonBufferdAsync(con).ToListAsync();

            Assert.Equal(ConnectionState.Open, con.State);
            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].Id);
            Assert.Equal("Data-1", list[0].Name);
            Assert.Equal(2, list[1].Id);
            Assert.Equal("Data-2", list[1].Name);
        }

        //--------------------------------------------------------------------------------
        // Cancel
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IQueryCancelAsyncAccessor
        {
            [Query]
            ValueTask<IList<DataEntity>> QueryBufferdAsync(CancellationToken cancel);

            [Query]
            IAsyncEnumerable<DataEntity> QueryNonBufferdAsync(CancellationToken cancel);
        }

        [Fact]
        public async ValueTask TestQueryBufferdCancelAsync()
        {
            await using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var accessor = generator.Create<IQueryCancelAsyncAccessor>();

                var list = await accessor.QueryBufferdAsync(default);

                Assert.Equal(2, list.Count);

                var cancel = new CancellationToken(true);
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await accessor.QueryBufferdAsync(cancel));
            }
        }

        [Fact]
        public async ValueTask TestQueryNonBufferdCancelAsync()
        {
            await using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var accessor = generator.Create<IQueryCancelAsyncAccessor>();

                var list = await accessor.QueryNonBufferdAsync(default).ToListAsync();

                Assert.Equal(2, list.Count);

                var cancel = new CancellationToken(true);
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await accessor.QueryNonBufferdAsync(cancel).ToListAsync(cancel));
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IQueryInvalidAccessor
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

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IQueryInvalidAccessor>());
        }

        [DataAccessor]
        public interface IQueryInvalidAsyncAccessor
        {
            [Query]
            ValueTask QueryAsync();
        }

        [Fact]
        public void TestQueryInvalidAsync()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IQueryInvalidAsyncAccessor>());
        }
    }
}
