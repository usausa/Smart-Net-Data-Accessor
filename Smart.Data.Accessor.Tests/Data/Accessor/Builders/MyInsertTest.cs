namespace Smart.Data.Accessor.Builders;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;
using Smart.Mock.Data;

public sealed class MyInsertTest
{
    //--------------------------------------------------------------------------------
    // Entity
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertEntityAccessor
    {
        [MyInsert]
        void Insert(DbConnection con, DataEntity entity);
    }

    [Fact]
    public void TestInsertEntity()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertEntityAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("INSERT INTO Data (Id, Name) VALUES (@p0, @p1)", c.CommandText);
            cmd.SetupResult(0);
        });

        accessor.Insert(con, new DataEntity { Id = 1, Name = "Data" });
    }

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertParameterAccessor
    {
        [MyInsert(typeof(DataEntity))]
        void InsertByType(DbConnection con, long id, string name);

        [MyInsert("Data")]
        void InsertByName(DbConnection con, long id, string name);
    }

    [Fact]
    public void TestInsertParameter()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertParameterAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("INSERT INTO Data (Id, Name) VALUES (@p0, @p1)", c.CommandText);
            cmd.SetupResult(0);
        });
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("INSERT INTO Data (Id, Name) VALUES (@p0, @p1)", c.CommandText);
            cmd.SetupResult(0);
        });

        accessor.InsertByType(con, 1, "Data");
        accessor.InsertByName(con, 1, "Data");
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertInvalidAccessor
    {
        [MyInsert]
        void Insert();
    }

    [Fact]
    public void TestInsertInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(generator.Create<IInsertInvalidAccessor>);
    }

    //--------------------------------------------------------------------------------
    // Ignore
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertIgnoreAccessor
    {
        [MyInsert(typeof(DataEntity), OnDuplicate = DuplicateBehavior.Ignore)]
        void InsertIgnore(DbConnection con, long id, string name);
    }

    [Fact]
    public void TestInsertIgnore()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertIgnoreAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("INSERT IGNORE INTO Data (Id, Name) VALUES (@p0, @p1)", c.CommandText);
            cmd.SetupResult(0);
        });

        accessor.InsertIgnore(con, 1, "Data");
    }

    //--------------------------------------------------------------------------------
    // InsertOrUpdate
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertOrUpdateAccessor
    {
        [MyInsert(OnDuplicate = DuplicateBehavior.Update)]
        void InsertOrUpdate(DbConnection con, MultiKeyEntity entity);
    }

    [Fact]
    public void TestInsertOrUpdate()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertOrUpdateAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c => Assert.Equal("INSERT INTO MultiKey (Key1, Key2, Type, Name) VALUES (@p0, @p1, @p2, @p3) ON DUPLICATE KEY UPDATE Type = @p2, Name = @p3", c.CommandText);
            cmd.SetupResult(0);
        });

        accessor.InsertOrUpdate(con, new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data" });
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertOrUpdateInvalidAccessor
    {
        [MyInsert(typeof(DataEntity), OnDuplicate = DuplicateBehavior.Update)]
        void InsertOrUpdate(DbConnection con, long id, string name);
    }

    [Fact]
    public void TestInsertOrUpdateInvalid()
    {
        var generator = new TestFactoryBuilder()
            .Build();

        Assert.Throws<BuilderException>(generator.Create<IInsertOrUpdateInvalidAccessor>);
    }
}
