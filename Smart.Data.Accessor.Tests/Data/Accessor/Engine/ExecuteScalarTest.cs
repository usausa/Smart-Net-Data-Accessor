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

        //--------------------------------------------------------------------------------
        // Result is null
        //--------------------------------------------------------------------------------

        [Fact]
        public void ExecuteScalarResultIsNull()
        {
            using (TestDatabase.Initialize())
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseMemoryDatabase()
                    .SetSql("SELECT NULL")
                    .Build();
                var dao = generator.Create<IExecuteScalarSimpleDao>();

                var count = dao.ExecuteScalar();

                Assert.Equal(0, count);
            }
        }

        [Fact]
        public async Task ExecuteScalarResultIsNullAsync()
        {
            using (TestDatabase.Initialize())
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
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

        [Dao]
        public interface IExecuteScalarObjectDao
        {
            [ExecuteScalar]
            object ExecuteScalar();
        }

        [Fact]
        public void ExecuteScalarObject()
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
                var dao = generator.Create<IExecuteScalarObjectDao>();

                var count = dao.ExecuteScalar();

                Assert.Equal(2L, count);
            }
        }

        [Dao]
        public interface IExecuteScalarObjectAsyncDao
        {
            [ExecuteScalar]
            Task<object> ExecuteScalarAsync();
        }

        [Fact]
        public async Task ExecuteScalarObjectAsync()
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
                var dao = generator.Create<IExecuteScalarObjectAsyncDao>();

                var count = await dao.ExecuteScalarAsync();

                Assert.Equal(2L, count);
            }
        }

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IExecuteScalarWithConvertDao
        {
            [ExecuteScalar]
            string ExecuteScalarWithConvert();
        }

        [Fact]
        public void ExecuteScalarWithConvert()
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
                var dao = generator.Create<IExecuteScalarWithConvertDao>();

                var count = dao.ExecuteScalarWithConvert();

                Assert.Equal("2", count);
            }
        }

        [Dao]
        public interface IExecuteScalarWithConvertAsyncDao
        {
            [ExecuteScalar]
            Task<string> ExecuteScalarWithConvertAsync();
        }

        [Fact]
        public async Task ExecuteScalarWithConvertAsync()
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
                var dao = generator.Create<IExecuteScalarWithConvertAsyncDao>();

                var count = await dao.ExecuteScalarWithConvertAsync();

                Assert.Equal("2", count);
            }
        }

        //--------------------------------------------------------------------------------
        // With Connection
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IExecuteScalarWithConnectionDao
        {
            [ExecuteScalar]
            long ExecuteScalar(DbConnection con);
        }

        [Fact]
        public void ExecuteScalarWithConnection()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IExecuteScalarWithConnectionDao>();

                con.Open();

                var count = dao.ExecuteScalar(con);

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(2, count);
            }
        }

        [Dao]
        public interface IExecuteScalarWithConnectionAsyncDao
        {
            [ExecuteScalar]
            Task<long> ExecuteScalarAsync(DbConnection con);
        }

        [Fact]
        public async Task ExecuteScalarWithConnectionAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
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

        [Dao]
        public interface IExecuteScalarCancelAsyncDao
        {
            [ExecuteScalar]
            Task<long> ExecuteScalarAsync(CancellationToken cancel);
        }

        [Fact]
        public async Task ExecuteScalarCancelAsync()
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

        [Dao]
        public interface IExecuteScalarInvalidDao
        {
            [ExecuteScalar]
            void ExecuteScalar();
        }

        [Fact]
        public void ExecuteScalarInvalid()
        {
            var generator = new GeneratorBuilder()
                .EnableDebug()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteScalarInvalidDao>());
        }

        [Dao]
        public interface IExecuteScalarInvalidAsyncDao
        {
            [ExecuteScalar]
            Task ExecuteScalarAsync();
        }

        [Fact]
        public void ExecuteScalarInvalidAsync()
        {
            var generator = new GeneratorBuilder()
                .EnableDebug()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteScalarInvalidAsyncDao>());
        }
    }
}
