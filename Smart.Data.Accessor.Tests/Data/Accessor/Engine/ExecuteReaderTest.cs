namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Generator;
using Smart.Mock;

public sealed class ExecuteReaderTest
{
    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteReaderSimpleAccessor
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

            var accessor = generator.Create<IExecuteReaderSimpleAccessor>();

            using var reader = accessor.ExecuteReader();
            Assert.True(reader.Read());
            Assert.True(reader.Read());
            Assert.False(reader.Read());
        }
    }

    [DataAccessor]
    public interface IExecuteReaderSimpleAsyncAccessor
    {
        [ExecuteReader]
        ValueTask<IDataReader> ExecuteReaderAsync();
    }

    [Fact]
    public async Task TestExecuteReaderSimpleAsync()
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

            var accessor = generator.Create<IExecuteReaderSimpleAsyncAccessor>();

            using var reader = await accessor.ExecuteReaderAsync();
            Assert.True(reader.Read());
            Assert.True(reader.Read());
            Assert.False(reader.Read());
        }
    }

    //--------------------------------------------------------------------------------
    // With Connection
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteReaderWithConnectionAccessor
    {
        [ExecuteReader]
        IDataReader ExecuteReader(DbConnection con);
    }

    [Fact]
    public void TestExecuteReaderWithConnection()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
        var generator = new TestFactoryBuilder()
            .SetSql("SELECT * FROM Data ORDER BY Id")
            .Build();

        con.Open();

        var accessor = generator.Create<IExecuteReaderWithConnectionAccessor>();

        Assert.Equal(ConnectionState.Open, con.State);

        using (var reader = accessor.ExecuteReader(con))
        {
            Assert.True(reader.Read());
            Assert.True(reader.Read());
            Assert.False(reader.Read());
        }

        Assert.Equal(ConnectionState.Open, con.State);
    }

    [DataAccessor]
    public interface IExecuteReaderWithConnectionAsyncAccessor
    {
        [ExecuteReader]
        ValueTask<IDataReader> ExecuteReaderAsync(DbConnection con);
    }

    [Fact]
    public async Task TestExecuteReaderWithConnectionAsync()
    {
        await using var con = TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" });
        var generator = new TestFactoryBuilder()
            .SetSql("SELECT * FROM Data ORDER BY Id")
            .Build();

        await con.OpenAsync();

        var accessor = generator.Create<IExecuteReaderWithConnectionAsyncAccessor>();

        Assert.Equal(ConnectionState.Open, con.State);

        using (var reader = await accessor.ExecuteReaderAsync(con))
        {
            Assert.True(reader.Read());
            Assert.True(reader.Read());
            Assert.False(reader.Read());
        }

        Assert.Equal(ConnectionState.Open, con.State);
    }

    //--------------------------------------------------------------------------------
    // Cancel
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteReaderCancelAsyncAccessor
    {
        [ExecuteReader]
        ValueTask<IDataReader> ExecuteReaderAsync(CancellationToken cancel);
    }

    [Fact]
    public async Task TestExecuteReaderCancelAsync()
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

            var accessor = generator.Create<IExecuteReaderCancelAsyncAccessor>();

            using (await accessor.ExecuteReaderAsync(default))
            {
            }

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                var cancel = new CancellationToken(true);
                using (await accessor.ExecuteReaderAsync(cancel))
                {
                }
            });
        }
    }

    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteReaderInvalidAccessor
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

        Assert.Throws<AccessorGeneratorException>(generator.Create<IExecuteReaderInvalidAccessor>);
    }

    [DataAccessor]
    public interface IExecuteReaderInvalidAsyncAccessor
    {
        [ExecuteReader]
        ValueTask ExecuteReaderAsync();
    }

    [Fact]
    public void TestExecuteReaderInvalidAsync()
    {
        var generator = new TestFactoryBuilder()
            .SetSql(string.Empty)
            .Build();

        Assert.Throws<AccessorGeneratorException>(generator.Create<IExecuteReaderInvalidAsyncAccessor>);
    }
}
