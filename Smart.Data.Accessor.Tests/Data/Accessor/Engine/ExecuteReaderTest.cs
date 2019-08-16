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

    public class ExecuteReaderTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteReaderSimpleDao
        {
            [ExecuteReader]
            IDataReader ExecuteReader();
        }

        [Fact]
        public void TestExecuteReaderSimple()
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

                var dao = generator.Create<IExecuteReaderSimpleDao>();

                using (var reader = dao.ExecuteReader())
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.Read());
                    Assert.False(reader.Read());
                }
            }
        }

        [DataAccessor]
        public interface IExecuteReaderSimpleAsyncDao
        {
            [ExecuteReader]
            Task<IDataReader> ExecuteReaderAsync();
        }

        [Fact]
        public async Task TestExecuteReaderSimpleAsync()
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

                var dao = generator.Create<IExecuteReaderSimpleAsyncDao>();

                using (var reader = await dao.ExecuteReaderAsync())
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.Read());
                    Assert.False(reader.Read());
                }
            }
        }

        //--------------------------------------------------------------------------------
        // With Connection
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteReaderWithConnectionDao
        {
            [ExecuteReader]
            IDataReader ExecuteReader(DbConnection con);
        }

        [Fact]
        public void TestExecuteReaderWithConnection()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();

                con.Open();

                var dao = generator.Create<IExecuteReaderWithConnectionDao>();

                Assert.Equal(ConnectionState.Open, con.State);

                using (var reader = dao.ExecuteReader(con))
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.Read());
                    Assert.False(reader.Read());
                }

                Assert.Equal(ConnectionState.Open, con.State);
            }
        }

        [DataAccessor]
        public interface IExecuteReaderWithConnectionAsyncDao
        {
            [ExecuteReader]
            Task<IDataReader> ExecuteReaderAsync(DbConnection con);
        }

        [Fact]
        public async Task TestExecuteReaderWithConnectionAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();

                con.Open();

                var dao = generator.Create<IExecuteReaderWithConnectionAsyncDao>();

                Assert.Equal(ConnectionState.Open, con.State);

                using (var reader = await dao.ExecuteReaderAsync(con))
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.Read());
                    Assert.False(reader.Read());
                }

                Assert.Equal(ConnectionState.Open, con.State);
            }
        }

        //--------------------------------------------------------------------------------
        // Cancel
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteReaderCancelAsyncDao
        {
            [ExecuteReader]
            Task<IDataReader> ExecuteReaderAsync(CancellationToken cancel);
        }

        [Fact]
        public async Task TestExecuteReaderCancelAsync()
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

                var dao = generator.Create<IExecuteReaderCancelAsyncDao>();

                using (await dao.ExecuteReaderAsync(default))
                {
                }

                await Assert.ThrowsAsync<TaskCanceledException>(async () =>
                {
                    var cancel = new CancellationToken(true);
                    using (await dao.ExecuteReaderAsync(cancel))
                    {
                    }
                });
            }
        }

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteReaderInvalidDao
        {
            [ExecuteReader]
            void ExecuteReader();
        }

        [Fact]
        public void TestExecuteReaderInvalid()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteReaderInvalidDao>());
        }

        [DataAccessor]
        public interface IExecuteReaderInvalidAsyncDao
        {
            [ExecuteReader]
            Task ExecuteReaderAsync();
        }

        [Fact]
        public void TestExecuteReaderInvalidAsync()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteReaderInvalidAsyncDao>());
        }
    }
}
