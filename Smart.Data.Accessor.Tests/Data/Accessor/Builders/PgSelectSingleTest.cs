namespace Smart.Data.Accessor.Builders;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;
using Smart.Mock.Data;

public sealed class PgSelectSingleTest
{
    //--------------------------------------------------------------------------------
    // Other
    //--------------------------------------------------------------------------------

    public sealed class OtherEntity
    {
        [Key(1)]
        public long Key1 { get; set; }

        [Key(2)]
        public long Key2 { get; set; }

        public string Name { get; set; } = default!;
    }

    [DataAccessor]
    public interface ISelectOtherAccessor
    {
        [PgSelectSingle(typeof(MultiKeyEntity))]
        OtherEntity? SelectByType(DbConnection con, OtherEntity key);

        [PgSelectSingle("MultiKey")]
        OtherEntity? SelectByName(DbConnection con, OtherEntity key);
    }

    [Fact]
    public void TestSelectOther()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<ISelectOtherAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey WHERE Key1 = @p0 AND Key2 = @p1", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey WHERE Key1 = @p0 AND Key2 = @p1", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });

        accessor.SelectByType(con, new OtherEntity { Key1 = 1, Key2 = 2 });
        accessor.SelectByName(con, new OtherEntity { Key1 = 1, Key2 = 2 });
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectInvalidAccessor
    {
        [PgSelectSingle("")]
        MultiKeyEntity? Select();
    }

    [Fact]
    public void TestSelectInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(generator.Create<ISelectInvalidAccessor>);
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectUpdateAccessor
    {
        [PgSelectSingle(ForUpdate = true)]
        MultiKeyEntity? Select(DbConnection con, long key1, long key2);
    }

    [Fact]
    public void TestSelectUpdate()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<ISelectUpdateAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey WHERE Key1 = @p0 AND Key2 = @p1 FOR UPDATE", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });

        accessor.Select(con, 1, 2);
    }
}
