namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Data.Common;

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
    public interface IExecuteScalarSimpleAccessor
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
            var accessor = generator.Create<IExecuteScalarSimpleAccessor>();

            var count = accessor.ExecuteScalar();

            Assert.Equal(2, count);
        }
    }

    [DataAccessor]
    public interface IExecuteScalarSimpleAsyncAccessor
    {
        [ExecuteScalar]
        ValueTask<long> ExecuteScalarAsync();
    }

    [Fact]
    public async ValueTask TestExecuteScalarSimpleAsync()
    {
        await using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT COUNT(*) FROM Data")
                .Build();
            var accessor = generator.Create<IExecuteScalarSimpleAsyncAccessor>();

            var count = await accessor.ExecuteScalarAsync();

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
            var accessor = generator.Create<IExecuteScalarSimpleAccessor>();

            var count = accessor.ExecuteScalar();

            Assert.Equal(0, count);
        }
    }

    [Fact]
    public async ValueTask TestExecuteScalarResultIsNullAsync()
    {
        await using (TestDatabase.Initialize())
        {
            var generator = new TestFactoryBuilder()
                .UseMemoryDatabase()
                .SetSql("SELECT NULL")
                .Build();
            var accessor = generator.Create<IExecuteScalarSimpleAsyncAccessor>();

            var count = await accessor.ExecuteScalarAsync();

            Assert.Equal(0, count);
        }
    }

    //--------------------------------------------------------------------------------
    // Result as object
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteScalarObjectAccessor
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
            var accessor = generator.Create<IExecuteScalarObjectAccessor>();

            var count = accessor.ExecuteScalar();

            Assert.Equal(2L, count);
        }
    }

    [DataAccessor]
    public interface IExecuteScalarObjectAsyncAccessor
    {
        [ExecuteScalar]
        ValueTask<object> ExecuteScalarAsync();
    }

    [Fact]
    public async ValueTask TestExecuteScalarObjectAsync()
    {
        await using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT COUNT(*) FROM Data")
                .Build();
            var accessor = generator.Create<IExecuteScalarObjectAsyncAccessor>();

            var count = await accessor.ExecuteScalarAsync();

            Assert.Equal(2L, count);
        }
    }

    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteScalarWithConvertAccessor
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
            var accessor = generator.Create<IExecuteScalarWithConvertAccessor>();

            var count = accessor.ExecuteScalarWithConvert();

            Assert.Equal("2", count);
        }
    }

    [DataAccessor]
    public interface IExecuteScalarWithConvertAsyncAccessor
    {
        [ExecuteScalar]
        ValueTask<string> ExecuteScalarWithConvertAsync();
    }

    [Fact]
    public async ValueTask TestExecuteScalarWithConvertAsync()
    {
        await using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT COUNT(*) FROM Data")
                .Build();
            var accessor = generator.Create<IExecuteScalarWithConvertAsyncAccessor>();

            var count = await accessor.ExecuteScalarWithConvertAsync();

            Assert.Equal("2", count);
        }
    }

    //--------------------------------------------------------------------------------
    // With Connection
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteScalarWithConnectionAccessor
    {
        [ExecuteScalar]
        long ExecuteScalar(DbConnection con);
    }

    [Fact]
    public void TestExecuteScalarWithConnection()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
        var generator = new TestFactoryBuilder()
            .SetSql("SELECT COUNT(*) FROM Data")
            .Build();
        var accessor = generator.Create<IExecuteScalarWithConnectionAccessor>();

        con.Open();

        var count = accessor.ExecuteScalar(con);

        Assert.Equal(ConnectionState.Open, con.State);
        Assert.Equal(2, count);
    }

    [DataAccessor]
    public interface IExecuteScalarWithConnectionAsyncAccessor
    {
        [ExecuteScalar]
        ValueTask<long> ExecuteScalarAsync(DbConnection con);
    }

    [Fact]
    public async ValueTask TestExecuteScalarWithConnectionAsync()
    {
        await using var con = TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
        var generator = new TestFactoryBuilder()
            .SetSql("SELECT COUNT(*) FROM Data")
            .Build();
        var accessor = generator.Create<IExecuteScalarWithConnectionAsyncAccessor>();

        await con.OpenAsync();

        var count = await accessor.ExecuteScalarAsync(con);

        Assert.Equal(ConnectionState.Open, con.State);
        Assert.Equal(2, count);
    }

    //--------------------------------------------------------------------------------
    // Cancel
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteScalarCancelAsyncAccessor
    {
        [ExecuteScalar]
        ValueTask<long> ExecuteScalarAsync(CancellationToken cancel);
    }

    [Fact]
    public async ValueTask TestExecuteScalarCancelAsync()
    {
        await using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT COUNT(*) FROM Data")
                .Build();
            var accessor = generator.Create<IExecuteScalarCancelAsyncAccessor>();

            var count = await accessor.ExecuteScalarAsync(default);

            Assert.Equal(2, count);

            var cancel = new CancellationToken(true);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await accessor.ExecuteScalarAsync(cancel));
        }
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteScalarInvalidAccessor
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

        Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteScalarInvalidAccessor>());
    }

    [DataAccessor]
    public interface IExecuteScalarInvalidAsyncAccessor
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

        Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteScalarInvalidAsyncAccessor>());
    }
}
