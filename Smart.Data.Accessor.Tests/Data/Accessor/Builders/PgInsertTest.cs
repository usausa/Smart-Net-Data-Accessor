namespace Smart.Data.Accessor.Builders;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;
using Smart.Mock.Data;

using Xunit;

public class PgInsertTest
{
    //--------------------------------------------------------------------------------
    // Entity
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertEntityAccessor
    {
        [PgInsert]
        void Insert(DbConnection con, DataEntity entity);
    }

    [Fact]
    public void TestInsertEntity()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertEntityAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("INSERT INTO Data (Id, Name) VALUES (@p0, @p1)", c.CommandText);
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
        [PgInsert(typeof(DataEntity))]
        void InsertByType(DbConnection con, long id, string name);

        [PgInsert("Data")]
        void InsertByName(DbConnection con, long id, string name);
    }

    [Fact]
    public void TestInsertParameter()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertParameterAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("INSERT INTO Data (Id, Name) VALUES (@p0, @p1)", c.CommandText);
            cmd.SetupResult(0);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("INSERT INTO Data (Id, Name) VALUES (@p0, @p1)", c.CommandText);
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
        [PgInsert]
        void Insert();
    }

    [Fact]
    public void TestInsertInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(() => generator.Create<IInsertInvalidAccessor>());
    }

    //--------------------------------------------------------------------------------
    // Ignore
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertIgnoreAccessor
    {
        [PgInsert(typeof(DataEntity), OnDuplicate = DuplicateBehavior.Ignore)]
        void InsertIgnore(DbConnection con, [Key] long id, string name);
    }

    [Fact]
    public void TestInsertIgnore()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertIgnoreAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("INSERT INTO Data (Id, Name) VALUES (@p0, @p1) ON CONFLICT (Id) DO NOTHING", c.CommandText);
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
        [PgInsert(OnDuplicate = DuplicateBehavior.Update)]
        void InsertOrUpdate(DbConnection con, MultiKeyEntity entity);
    }

    [Fact]
    public void TestInsertOrUpdate()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IInsertOrUpdateAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal("INSERT INTO MultiKey (Key1, Key2, Type, Name) VALUES (@p0, @p1, @p2, @p3) ON CONFLICT (Key1, Key2) DO UPDATE SET Type = @p2, Name = @p3", c.CommandText);
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
        [PgInsert(typeof(DataEntity), OnDuplicate = DuplicateBehavior.Update)]
        void InsertOrUpdate(DbConnection con, long id, string name);
    }

    [Fact]
    public void TestInsertOrUpdateInvalid()
    {
        var generator = new TestFactoryBuilder()
            .Build();

        Assert.Throws<BuilderException>(() => generator.Create<IInsertOrUpdateInvalidAccessor>());
    }
}
