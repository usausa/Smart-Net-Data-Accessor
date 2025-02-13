namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Generator;
using Smart.Mock;

#pragma warning disable xUnit1051
public sealed class QueryFirstOrDefaultTest
{
    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    [Optimize(true)]
    [DataAccessor]
    public interface IQueryFirstOrDefaultSimpleAccessor
    {
        [QueryFirstOrDefault]
        DataEntity? QueryFirstOrDefault(long id);
    }

    [Fact]
    public void TestQueryFirstOrDefaultSimple()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
                .Build();
            var accessor = generator.Create<IQueryFirstOrDefaultSimpleAccessor>();

            var entity = accessor.QueryFirstOrDefault(1L);

            AssertEx.NotNull(entity);
            Assert.Equal(1, entity.Id);
            Assert.Equal("Data-1", entity.Name);

            entity = accessor.QueryFirstOrDefault(2L);
            Assert.Null(entity);
        }
    }

    [Optimize(true)]
    [DataAccessor]
    public interface IQueryFirstOrDefaultSimpleAsyncAccessor
    {
        [QueryFirstOrDefault]
        ValueTask<DataEntity?> QueryFirstOrDefaultAsync(long id);
    }

    [Fact]
    public async Task TestQueryFirstOrDefaultSimpleAsync()
    {
        await using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
                .Build();
            var accessor = generator.Create<IQueryFirstOrDefaultSimpleAsyncAccessor>();

            var entity = await accessor.QueryFirstOrDefaultAsync(1L);

            AssertEx.NotNull(entity);
            Assert.Equal(1, entity.Id);
            Assert.Equal("Data-1", entity.Name);

            entity = await accessor.QueryFirstOrDefaultAsync(2L);
            Assert.Null(entity);
        }
    }

    //--------------------------------------------------------------------------------
    // With Connection
    //--------------------------------------------------------------------------------

    [Optimize(true)]
    [DataAccessor]
    public interface IQueryFirstOrDefaultWithConnectionAccessor
    {
        [QueryFirstOrDefault]
        DataEntity? QueryFirstOrDefault(DbConnection con, long id);
    }

    [Fact]
    public void TestQueryFirstOrDefaultWithConnection()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" });
        var generator = new TestFactoryBuilder()
            .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
            .Build();
        var accessor = generator.Create<IQueryFirstOrDefaultWithConnectionAccessor>();

        con.Open();

        var entity = accessor.QueryFirstOrDefault(con, 1L);

        Assert.Equal(ConnectionState.Open, con.State);
        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("Data-1", entity.Name);

        entity = accessor.QueryFirstOrDefault(con, 2L);
        Assert.Null(entity);
    }

    [Optimize(true)]
    [DataAccessor]
    public interface IQueryFirstOrDefaultWithConnectionAsyncAccessor
    {
        [QueryFirstOrDefault]
        ValueTask<DataEntity?> QueryFirstOrDefaultAsync(DbConnection con, long id);
    }

    [Fact]
    public async Task TestQueryFirstOrDefaultWithConnectionAsync()
    {
        await using var con = TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" });
        var generator = new TestFactoryBuilder()
            .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
            .Build();
        var accessor = generator.Create<IQueryFirstOrDefaultWithConnectionAsyncAccessor>();

        await con.OpenAsync();

        var entity = await accessor.QueryFirstOrDefaultAsync(con, 1L);

        Assert.Equal(ConnectionState.Open, con.State);
        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("Data-1", entity.Name);

        entity = await accessor.QueryFirstOrDefaultAsync(con, 2L);
        Assert.Null(entity);
    }

    //--------------------------------------------------------------------------------
    // Cancel
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IQueryFirstOrDefaultCancelAsyncAccessor
    {
        [QueryFirstOrDefault]
        ValueTask<DataEntity?> QueryFirstOrDefaultAsync(long id, CancellationToken cancel);
    }

    [Fact]
    public async Task TestQueryFirstOrDefaultCancelAsync()
    {
        await using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data WHERE Id = /*@ id */1")
                .Build();
            var accessor = generator.Create<IQueryFirstOrDefaultCancelAsyncAccessor>();

            var entity = await accessor.QueryFirstOrDefaultAsync(1L, default);

            AssertEx.NotNull(entity);

            var cancel = new CancellationToken(true);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await accessor.QueryFirstOrDefaultAsync(1L, cancel));
        }
    }

    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IQueryFirstOrDefaultInvalidAccessor
    {
        [QueryFirstOrDefault]
        void QueryFirstOrDefault();
    }

    [Fact]
    public void TestQueryFirstOrDefaultInvalid()
    {
        var generator = new TestFactoryBuilder()
            .SetSql(string.Empty)
            .Build();

        Assert.Throws<AccessorGeneratorException>(generator.Create<IQueryFirstOrDefaultInvalidAccessor>);
    }

    [DataAccessor]
    public interface IQueryFirstOrDefaultInvalidAsyncAccessor
    {
        [QueryFirstOrDefault]
        ValueTask QueryFirstOrDefaultAsync();
    }

    [Fact]
    public void TestQueryFirstOrDefaultInvalidAsync()
    {
        var generator = new TestFactoryBuilder()
            .SetSql(string.Empty)
            .Build();

        Assert.Throws<AccessorGeneratorException>(generator.Create<IQueryFirstOrDefaultInvalidAsyncAccessor>);
    }
}
