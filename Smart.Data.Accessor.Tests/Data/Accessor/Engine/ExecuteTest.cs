namespace Smart.Data.Accessor.Engine;

using System.Data;
using System.Data.Common;

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
    public interface IExecuteSimpleAccessor
    {
        [Execute]
        int Execute(long id, string name);
    }

    [Fact]
    public void TestExecuteSimple()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
            .Build();
        var accessor = generator.Create<IExecuteSimpleAccessor>();

        var effect = accessor.Execute(2, "xxx");

        Assert.Equal(1, effect);

        var entity = con.QueryData(2);
        AssertEx.NotNull(entity);
        Assert.Equal(2, entity.Id);
        Assert.Equal("xxx", entity.Name);
    }

    [DataAccessor]
    public interface IExecuteSimpleAsyncAccessor
    {
        [Execute]
        ValueTask<int> ExecuteAsync(long id, string name);
    }

    [Fact]
    public async Task TestExecuteSimpleAsync()
    {
        await using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
            .Build();
        var accessor = generator.Create<IExecuteSimpleAsyncAccessor>();

        var effect = await accessor.ExecuteAsync(2, "xxx");

        Assert.Equal(1, effect);

        var entity = con.QueryData(2);
        AssertEx.NotNull(entity);
        Assert.Equal(2, entity.Id);
        Assert.Equal("xxx", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // Result void
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteVoidAccessor
    {
        [Execute]
        void Execute(long id, string name);
    }

    [Fact]
    public void TestExecuteVoid()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
            .Build();
        var accessor = generator.Create<IExecuteVoidAccessor>();

        accessor.Execute(2, "xxx");

        var entity = con.QueryData(2);
        AssertEx.NotNull(entity);
    }

    [DataAccessor]
    public interface IExecuteVoidAsyncAccessor
    {
        [Execute]
        ValueTask ExecuteAsync(long id, string name);
    }

    [Fact]
    public async Task TestExecuteVoidAsync()
    {
        await using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
            .Build();
        var accessor = generator.Create<IExecuteVoidAsyncAccessor>();

        await accessor.ExecuteAsync(2, "xxx");

        var entity = con.QueryData(2);
        AssertEx.NotNull(entity);
    }

    //--------------------------------------------------------------------------------
    // With Connection
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteWithConnectionAccessor
    {
        [Execute]
        int Execute(DbConnection con, long id, string name);
    }

    [Fact]
    public void TestExecuteWithConnection()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
            .Build();
        var accessor = generator.Create<IExecuteWithConnectionAccessor>();

        con.Open();

        var effect = accessor.Execute(con, 2, "xxx");

        Assert.Equal(ConnectionState.Open, con.State);
        Assert.Equal(1, effect);

        var entity = con.QueryData(2);
        AssertEx.NotNull(entity);
        Assert.Equal(2, entity.Id);
        Assert.Equal("xxx", entity.Name);
    }

    [DataAccessor]
    public interface IExecuteWithConnectionAsyncAccessor
    {
        [Execute]
        ValueTask<int> ExecuteAsync(DbConnection con, long id, string name);
    }

    [Fact]
    public async Task TestExecuteWithConnectionAsync()
    {
        await using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
            .Build();
        var accessor = generator.Create<IExecuteWithConnectionAsyncAccessor>();

        await con.OpenAsync();

        var effect = await accessor.ExecuteAsync(con, 2, "xxx");

        Assert.Equal(ConnectionState.Open, con.State);
        Assert.Equal(1, effect);

        var entity = con.QueryData(2);
        AssertEx.NotNull(entity);
        Assert.Equal(2, entity.Id);
        Assert.Equal("xxx", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // Cancel
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteCancelAsyncAccessor
    {
        [Execute]
        ValueTask<int> ExecuteAsync(long id, string name, CancellationToken cancel);
    }

    [Fact]
    public async Task TestExecuteCancelAsync()
    {
        await using (TestDatabase.Initialize()
            .SetupDataTable())
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                .Build();
            var accessor = generator.Create<IExecuteCancelAsyncAccessor>();

            var effect = await accessor.ExecuteAsync(2, "xxx", default);

            Assert.Equal(1, effect);

            var cancel = new CancellationToken(true);
            await Assert.ThrowsAsync<TaskCanceledException>(async () => await accessor.ExecuteAsync(2, "xxx", cancel));
        }
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IExecuteInvalidAccessor
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

        Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteInvalidAsyncAccessor>());
    }

    [DataAccessor]
    public interface IExecuteInvalidAsyncAccessor
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

        Assert.Throws<AccessorGeneratorException>(() => generator.Create<IExecuteInvalidAsyncAccessor>());
    }
}
