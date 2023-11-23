namespace Smart.Data.Accessor;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;

using Xunit;

public class InSqlTest
{
    //--------------------------------------------------------------------------------
    // Array
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInArrayAccessor
    {
        [Query]
        IList<DataEntity> QueryData(int[]? ids);
    }

    [Fact]
    public void TestArray()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" })
            .InsertData(new DataEntity { Id = 3, Name = "Data-3" })
            .InsertData(new DataEntity { Id = 4, Name = "Data-4" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data WHERE Id IN /*@ ids */(2, 4)")
                .Build();
            var accessor = generator.Create<IInArrayAccessor>();

            var list = accessor.QueryData(null);

            Assert.Empty(list);

            list = accessor.QueryData(Array.Empty<int>());

            Assert.Empty(list);

            list = accessor.QueryData([2, 4]);

            Assert.Equal(2, list.Count);

            list = accessor.QueryData(Enumerable.Range(1, 257).ToArray());

            Assert.Equal(4, list.Count);
        }
    }

    [DataAccessor]
    public interface IInArrayMixedAccessor
    {
        [Query]
        IList<DataEntity> QueryData(int[] ids, string name);
    }

    [Fact]
    public void TestArrayMixed()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "AAA" })
            .InsertData(new DataEntity { Id = 2, Name = "AAA" })
            .InsertData(new DataEntity { Id = 3, Name = "BBB" })
            .InsertData(new DataEntity { Id = 4, Name = "BBB" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data WHERE Id IN /*@ ids */(2, 4) AND Name = /*@ name */'AAA'")
                .Build();
            var accessor = generator.Create<IInArrayMixedAccessor>();

            var list = accessor.QueryData([2, 4], "AAA");

            Assert.Single(list);
        }
    }

    //--------------------------------------------------------------------------------
    // Array
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInListAccessor
    {
        [Query]
        IList<DataEntity> QueryData(List<int>? ids);
    }

    [Fact]
    public void TestList()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" })
            .InsertData(new DataEntity { Id = 3, Name = "Data-3" })
            .InsertData(new DataEntity { Id = 4, Name = "Data-4" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data WHERE Id IN /*@ ids */(2, 4)")
                .Build();
            var accessor = generator.Create<IInListAccessor>();

            var list = accessor.QueryData(null);

            Assert.Empty(list);

            list = accessor.QueryData(new List<int>());

            Assert.Empty(list);

            list = accessor.QueryData(new List<int> { 2, 4 });

            Assert.Equal(2, list.Count);

            list = accessor.QueryData(Enumerable.Range(1, 257).ToList());

            Assert.Equal(4, list.Count);
        }
    }

    [DataAccessor]
    public interface IInListMixedAccessor
    {
        [Query]
        IList<DataEntity> QueryData(List<int> ids, string name);
    }

    [Fact]
    public void TestListMixed()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "AAA" })
            .InsertData(new DataEntity { Id = 2, Name = "AAA" })
            .InsertData(new DataEntity { Id = 3, Name = "BBB" })
            .InsertData(new DataEntity { Id = 4, Name = "BBB" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data WHERE Id IN /*@ ids */(2, 4) AND Name = /*@ name */'AAA'")
                .Build();
            var accessor = generator.Create<IInListMixedAccessor>();

            var list = accessor.QueryData(new List<int> { 2, 4 }, "AAA");

            Assert.Single(list);
        }
    }
}
