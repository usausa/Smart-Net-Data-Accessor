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

    public class ExecuteScalarTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteScalarSimpleDao
        {
            [ExecuteScalar]
            long ExecuteScalar();
        }

        [Fact]
        public void TestExecuteScalarSimple()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarSimpleDao>();

                var count = dao.ExecuteScalar();

                Assert.Equal(2, count);
            }
        }

        [DataAccessor]
        public interface IExecuteScalarSimpleAsyncDao
        {
            [ExecuteScalar]
            ValueTask<long> ExecuteScalarAsync();
        }

        [Fact]
        public async ValueTask TestExecuteScalarSimpleAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarSimpleAsyncDao>();

                var count = await dao.ExecuteScalarAsync();

                Assert.Equal(2, count);
            }
        }

        //--------------------------------------------------------------------------------
        // Result is null
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestExecuteScalarResultIsNull()
        {
            using (TestDatabase.Initialize())
            {
                var generator = new TestFactoryBuilder()
                    .UseMemoryDatabase()
                    .SetSql("SELECT NULL")
                    .Build();
                var dao = generator.Create<IExecuteScalarSimpleDao>();

                var count = dao.ExecuteScalar();

                Assert.Equal(0, count);
            }
        }

        [Fact]
        public async ValueTask TestExecuteScalarResultIsNullAsync()
        {
            using (TestDatabase.Initialize())
            {
                var generator = new TestFactoryBuilder()
                    .UseMemoryDatabase()
                    .SetSql("SELECT NULL")
                    .Build();
                var dao = generator.Create<IExecuteScalarSimpleAsyncDao>();

                var count = await dao.ExecuteScalarAsync();

                Assert.Equal(0, count);
            }
        }

        //--------------------------------------------------------------------------------
        // Result as object
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteScalarObjectDao
        {
            [ExecuteScalar]
            object ExecuteScalar();
        }

        [Fact]
        public void TestExecuteScalarObject()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarObjectDao>();

                var count = dao.ExecuteScalar();

                Assert.Equal(2L, count);
            }
        }

        [DataAccessor]
        public interface IExecuteScalarObjectAsyncDao
        {
            [ExecuteScalar]
            ValueTask<object> ExecuteScalarAsync();
        }

        [Fact]
        public async ValueTask TestExecuteScalarObjectAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarObjectAsyncDao>();

                var count = await dao.ExecuteScalarAsync();

                Assert.Equal(2L, count);
            }
        }

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteScalarWithConvertDao
        {
            [ExecuteScalar]
            string ExecuteScalarWithConvert();
        }

        [Fact]
        public void TestExecuteScalarWithConvert()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarWithConvertDao>();

                var count = dao.ExecuteScalarWithConvert();

                Assert.Equal("2", count);
            }
        }

        [DataAccessor]
        public interface IExecuteScalarWithConvertAsyncDao
        {
            [ExecuteScalar]
            ValueTask<string> ExecuteScalarWithConvertAsync();
        }

        [Fact]
        public async ValueTask TestExecuteScalarWithConvertAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarWithConvertAsyncDao>();

                var count = await dao.ExecuteScalarWithConvertAsync();

                Assert.Equal("2", count);
            }
        }

        //--------------------------------------------------------------------------------
        // With Connection
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteScalarWithConnectionDao
        {
            [ExecuteScalar]
            long ExecuteScalar(DbConnection con);
        }

        [Fact]
        public void TestExecuteScalarWithConnection()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarWithConnectionDao>();

                con.Open();

                var count = dao.ExecuteScalar(con);

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(2, count);
            }
        }

        [DataAccessor]
        public interface IExecuteScalarWithConnectionAsyncDao
        {
            [ExecuteScalar]
            ValueTask<long> ExecuteScalarAsync(DbConnection con);
        }

        [Fact]
        public async ValueTask TestExecuteScalarWithConnectionAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarWithConnectionAsyncDao>();

                con.Open();

                var count = await dao.ExecuteScalarAsync(con);

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(2, count);
            }
        }

        //--------------------------------------------------------------------------------
        // Cancel
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteScalarCancelAsyncDao
        {
            [ExecuteScalar]
            ValueTask<long> ExecuteScalarAsync(CancellationToken cancel);
        }

        [Fact]
        public async ValueTask TestExecuteScalarCancelAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarCancelAsyncDao>();

                var count = await dao.ExecuteScalarAsync(default);

                Assert.Equal(2, count);

                var cancel = new CancellationToken(true);
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await dao.ExecuteScalarAsync(cancel));
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteScalarInvalidDao
        {
            [ExecuteScalar]
            void ExecuteScalar();
        }

        [Fact]
        public void TestExecuteScalarInvalid()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteScalarInvalidDao>());
        }

        [DataAccessor]
        public interface IExecuteScalarInvalidAsyncDao
        {
            [ExecuteScalar]
            ValueTask ExecuteScalarAsync();
        }

        [Fact]
        public void TestExecuteScalarInvalidAsync()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteScalarInvalidAsyncDao>());
        }
    }
}
