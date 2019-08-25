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

    public class ExecuteTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteSimpleDao
        {
            [Execute]
            int Execute(long id, string name);
        }

        [Fact]
        public void TestExecuteSimple()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteSimpleDao>();

                var effect = dao.Execute(2, "xxx");

                Assert.Equal(1, effect);

                var entity = con.QueryData(2);
                Assert.NotNull(entity);
                Assert.Equal(2, entity.Id);
                Assert.Equal("xxx", entity.Name);
            }
        }

        [DataAccessor]
        public interface IExecuteSimpleAsyncDao
        {
            [Execute]
            ValueTask<int> ExecuteAsync(long id, string name);
        }

        [Fact]
        public async ValueTask TestExecuteSimpleAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteSimpleAsyncDao>();

                var effect = await dao.ExecuteAsync(2, "xxx");

                Assert.Equal(1, effect);

                var entity = con.QueryData(2);
                Assert.NotNull(entity);
                Assert.Equal(2, entity.Id);
                Assert.Equal("xxx", entity.Name);
            }
        }

        //--------------------------------------------------------------------------------
        // Result void
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteVoidDao
        {
            [Execute]
            void Execute(long id, string name);
        }

        [Fact]
        public void TestExecuteVoid()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteVoidDao>();

                dao.Execute(2, "xxx");

                var entity = con.QueryData(2);
                Assert.NotNull(entity);
            }
        }

        [DataAccessor]
        public interface IExecuteVoidAsyncDao
        {
            [Execute]
            ValueTask ExecuteAsync(long id, string name);
        }

        [Fact]
        public async ValueTask TestExecuteVoidAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteVoidAsyncDao>();

                await dao.ExecuteAsync(2, "xxx");

                var entity = con.QueryData(2);
                Assert.NotNull(entity);
            }
        }

        //--------------------------------------------------------------------------------
        // With Connection
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteWithConnectionDao
        {
            [Execute]
            int Execute(DbConnection con, long id, string name);
        }

        [Fact]
        public void TestExecuteWithConnection()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteWithConnectionDao>();

                con.Open();

                var effect = dao.Execute(con, 2, "xxx");

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(1, effect);

                var entity = con.QueryData(2);
                Assert.NotNull(entity);
                Assert.Equal(2, entity.Id);
                Assert.Equal("xxx", entity.Name);
            }
        }

        [DataAccessor]
        public interface IExecuteWithConnectionAsyncDao
        {
            [Execute]
            ValueTask<int> ExecuteAsync(DbConnection con, long id, string name);
        }

        [Fact]
        public async ValueTask TestExecuteWithConnectionAsync()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteWithConnectionAsyncDao>();

                con.Open();

                var effect = await dao.ExecuteAsync(con, 2, "xxx");

                Assert.Equal(ConnectionState.Open, con.State);
                Assert.Equal(1, effect);

                var entity = con.QueryData(2);
                Assert.NotNull(entity);
                Assert.Equal(2, entity.Id);
                Assert.Equal("xxx", entity.Name);
            }
        }

        //--------------------------------------------------------------------------------
        // Cancel
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteCancelAsyncDao
        {
            [Execute]
            ValueTask<int> ExecuteAsync(long id, string name, CancellationToken cancel);
        }

        [Fact]
        public async ValueTask TestExecuteCancelAsync()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteCancelAsyncDao>();

                var effect = await dao.ExecuteAsync(2, "xxx", default);

                Assert.Equal(1, effect);

                var cancel = new CancellationToken(true);
                await Assert.ThrowsAsync<TaskCanceledException>(async () => await dao.ExecuteAsync(2, "xxx", cancel));
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IExecuteInvalidDao
        {
            [Execute]
            string Execute();
        }

        [Fact]
        public void TestExecuteInvalid()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteInvalidAsyncDao>());
        }

        [DataAccessor]
        public interface IExecuteInvalidAsyncDao
        {
            [Execute]
            ValueTask<string> ExecuteAsync();
        }

        [Fact]
        public void TestExecuteInvalidAsync()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteInvalidAsyncDao>());
        }
    }
}
