namespace Smart.Data.Accessor.Builders;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;
using Smart.Mock.Data;

using Xunit;

public class SqlSelectTest
{
    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectOrderAccessor
    {
        [SqlSelect]
        List<MultiKeyEntity> SelectKeyOrder(DbConnection con);

        [SqlSelect(Order = "Name DESC")]
        List<MultiKeyEntity> SelectCustomOrder(DbConnection con);

        [SqlSelect]
        List<MultiKeyEntity> SelectParameterOrder(DbConnection con, [Order] string order);
    }

    [Fact]
    public void TestSelectOrder()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<ISelectOrderAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey ORDER BY Key1, Key2", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey ORDER BY Name DESC", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey ORDER BY Name DESC", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });

        accessor.SelectKeyOrder(con);
        accessor.SelectCustomOrder(con);
        accessor.SelectParameterOrder(con, "Name DESC");
    }

    //--------------------------------------------------------------------------------
    // Other
    //--------------------------------------------------------------------------------

    public class OtherEntity
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
        [SqlSelect(typeof(MultiKeyEntity))]
        List<OtherEntity> SelectByType(DbConnection con);

        [SqlSelect("MultiKey")]
        List<OtherEntity> SelectByName(DbConnection con);
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
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey ORDER BY Key1, Key2", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey ORDER BY Key1, Key2", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });

        accessor.SelectByType(con);
        accessor.SelectByName(con);
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectInvalidAccessor
    {
        [SqlSelect("")]
        List<MultiKeyEntity> Select();
    }

    [Fact]
    public void TestSelectInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(() => generator.Create<ISelectInvalidAccessor>());
    }

    //--------------------------------------------------------------------------------
    // Paging
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectTopAccessor
    {
        [SqlSelect(Top = 10)]
        List<MultiKeyEntity> Select(DbConnection con);
    }

    [Fact]
    public void TestSelectTop()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<ISelectTopAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("SELECT TOP 10 * FROM MultiKey ORDER BY Key1, Key2", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });

        accessor.Select(con);
    }

    [DataAccessor]
    public interface ISelectPagingAccessor
    {
        [SqlSelect]
        List<MultiKeyEntity> Select(DbConnection con, [Limit] int limit, [Offset] int offset);
    }

    [Fact]
    public void TestSelectPaging()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<ISelectPagingAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal("SELECT * FROM MultiKey ORDER BY Key1, Key2 LIMIT @_p0 OFFSET @_p1", c.CommandText);
                Assert.Equal(10, c.Parameters[0].Value);
                Assert.Equal(20, c.Parameters[1].Value);
            };
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });

        accessor.Select(con, 10, 20);
    }

    //--------------------------------------------------------------------------------
    // Update
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectUpdateAccessor
    {
        [SqlSelect(ForUpdate = true)]
        List<MultiKeyEntity> Select(DbConnection con);
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
            cmd.Executing = c => Assert.Equal("SELECT * FROM MultiKey WITH (UPDLOCK, HOLDLOCK) ORDER BY Key1, Key2", c.CommandText);
            cmd.SetupResult(new MockDataReader(MultiKeyEntity.Columns, new List<object[]>()));
        });

        accessor.Select(con);
    }
}
