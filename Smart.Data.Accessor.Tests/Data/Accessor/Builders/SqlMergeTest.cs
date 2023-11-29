namespace Smart.Data.Accessor.Builders;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;
using Smart.Mock.Data;

using Xunit;

public class SqlMergeTest
{
    //--------------------------------------------------------------------------------
    // Entity
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IMergeEntityAccessor
    {
        [SqlMerge]
        void Merge(DbConnection con, MultiKeyEntity entity);
    }

    [Fact]
    public void TestMergeEntity()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IMergeEntityAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal(
                "MERGE INTO MultiKey _T0 USING (SELECT @p0 AS Key1, @p1 AS Key2) AS _T1 ON (_T0.Key1 = _T1.Key1 AND _T0.Key2 = _T1.Key2) " +
                "WHEN NOT MATCHED THEN INSERT (Key1, Key2, Type, Name) VALUES (@p0, @p1, @p2, @p3) " +
                "WHEN MATCHED THEN UPDATE SET Type = @p2, Name = @p3",
                c.CommandText);
            cmd.SetupResult(0);
        });

        accessor.Merge(con, new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data" });
    }

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IMergeParameterAccessor
    {
        [SqlMerge(typeof(DataEntity))]
        void MergeByType(DbConnection con, [Key] long id, string name);

        [SqlMerge("Data")]
        void MergeByName(DbConnection con, [Key] long id, string name);
    }

    [Fact]
    public void TestMergeParameter()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        var accessor = generator.Create<IMergeParameterAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal(
                "MERGE INTO Data _T0 USING (SELECT @p0 AS Id) AS _T1 ON (_T0.Id = _T1.Id) " +
                "WHEN NOT MATCHED THEN INSERT (Id, Name) VALUES (@p0, @p1) " +
                "WHEN MATCHED THEN UPDATE SET Name = @p1",
                c.CommandText);
            cmd.SetupResult(0);
        });
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c => Assert.Equal(
                "MERGE INTO Data _T0 USING (SELECT @p0 AS Id) AS _T1 ON (_T0.Id = _T1.Id) " +
                "WHEN NOT MATCHED THEN INSERT (Id, Name) VALUES (@p0, @p1) " +
                "WHEN MATCHED THEN UPDATE SET Name = @p1",
                c.CommandText);
            cmd.SetupResult(0);
        });

        accessor.MergeByType(con, 1, "Data");
        accessor.MergeByName(con, 1, "Data");
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IMergeInvalidAccessor
    {
        [SqlMerge]
        void Merge();
    }

    [Fact]
    public void TestMergeInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(generator.Create<IMergeInvalidAccessor>);
    }
}
