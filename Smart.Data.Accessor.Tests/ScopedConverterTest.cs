namespace Smart.Data.Accessor.Tests;

using System.Data;

using Smart.Data.Accessor.Tests.Accessors;
using Smart.Mock.Data;

using Xunit;

// Converter scope chain: method / class / profile scope for [TypeHandler<>],
// plus writer-side ToDb and scalar-return FromDb. TicksConverter maps DateTime <-> Int64 ticks.
public sealed class ScopedConverterTest
{
    private static readonly DateTime Expected = new(2026, 6, 2, 9, 0, 0, DateTimeKind.Utc);

    private static MockDataReader TimestampReader() =>
        new(
            [new MockColumn(typeof(long), "Id"), new MockColumn(typeof(long), "CreatedAt")],
            new List<object[]> { new object[] { 7L, Expected.Ticks } });

    [Fact]
    public void ClassScopeConverterAppliesOnRead()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(cmd => cmd.SetupResult(TimestampReader()));

        var entity = Assert.Single(new ClassScopeConverterAccessor().QueryAll(con));
        Assert.Equal(7L, entity.Id);
        Assert.Equal(Expected, entity.CreatedAt);
        Assert.Equal(DateTimeKind.Utc, entity.CreatedAt.Kind);
    }

    [Fact]
    public void MethodScopeConverterAppliesOnRead()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(cmd => cmd.SetupResult(TimestampReader()));

        var entity = Assert.Single(new MethodScopeConverterAccessor().QueryAll(con));
        Assert.Equal(Expected, entity.CreatedAt);
    }

    [Fact]
    public void ProfileScopeConverterAppliesOnRead()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(cmd => cmd.SetupResult(TimestampReader()));

        var entity = Assert.Single(new ProfileScopeConverterAccessor().QueryAll(con));
        Assert.Equal(Expected, entity.CreatedAt);
    }

    [Fact]
    public void ClassScopeConverterAppliesOnWrite()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                // VALUES (id, createdAt): createdAt is the second bound parameter, written via ToDb.
                var p = (MockDbParameter)c.Parameters[1]!;
                Assert.Equal(Expected.Ticks, (long)p.Value!);
            };
            cmd.SetupResult(1);
        });

        Assert.Equal(1, new ClassScopeConverterAccessor().Insert(con, 7L, Expected));
    }

    [Fact]
    public void ClassScopeConverterAppliesOnScalarReturn()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(cmd => cmd.SetupResult(Expected.Ticks));

        Assert.Equal(Expected, new ClassScopeConverterAccessor().MaxCreatedAt(con));
    }

    [Fact]
    public void ClassScopeTypeMapSetsParameterDbType()
    {
        using var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                var p = (MockDbParameter)c.Parameters[0]!;
                Assert.Equal(DbType.AnsiString, p.DbType);
            };
            cmd.SetupResult(1);
        });

        Assert.Equal(1, new TypeMapScopeAccessor().Insert(con, "Alice"));
    }
}
