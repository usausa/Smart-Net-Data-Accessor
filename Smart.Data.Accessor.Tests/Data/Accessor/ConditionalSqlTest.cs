namespace Smart.Data.Accessor;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;

public sealed class ConditionalSqlTest
{
    //--------------------------------------------------------------------------------
    // Execute
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IDynamicAccessor
    {
        [Query]
        IList<DataEntity> QueryData(int? id);
    }

    [Fact]
    public void TestHelper()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" })
            .InsertData(new DataEntity { Id = 3, Name = "Data-3" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql(
                    "SELECT * FROM Data\r\n" +
                    "/*% if (IsNotNull(id)) { */\r\n" +
                    "WHERE Id >= /*@ id */0\r\n" +
                    "/*% } */\r\n")
                .Build();
            var accessor = generator.Create<IDynamicAccessor>();

            var list = accessor.QueryData(null);

            Assert.Equal(3, list.Count);

            list = accessor.QueryData(2);

            Assert.Equal(2, list.Count);
        }
    }

    //--------------------------------------------------------------------------------
    // Custom
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestCustomHelper()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" })
            .InsertData(new DataEntity { Id = 3, Name = "Data-3" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql(
                    "/*!helper Smart.Mock.CustomScriptHelper */\r\n" +
                    "SELECT * FROM Data\r\n" +
                    "/*% if (HasValue(id)) { */\r\n" +
                    "WHERE Id >= /*@ id */0\r\n" +
                    "/*% } */\r\n")
                .Build();
            var accessor = generator.Create<IDynamicAccessor>();

            var list = accessor.QueryData(null);

            Assert.Equal(3, list.Count);

            list = accessor.QueryData(2);

            Assert.Equal(2, list.Count);
        }
    }

    [Fact]
    public void TestCustomUsing()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
            .InsertData(new DataEntity { Id = 2, Name = "Data-2" })
            .InsertData(new DataEntity { Id = 3, Name = "Data-3" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql(
                    "/*!using Smart.Mock */\r\n" +
                    "SELECT * FROM Data\r\n" +
                    "/*% if (CustomScriptHelper.HasValue(id)) { */\r\n" +
                    "WHERE Id >= /*@ id */0\r\n" +
                    "/*% } */\r\n")
                .Build();
            var accessor = generator.Create<IDynamicAccessor>();

            var list = accessor.QueryData(null);

            Assert.Equal(3, list.Count);

            list = accessor.QueryData(2);

            Assert.Equal(2, list.Count);
        }
    }

    //--------------------------------------------------------------------------------
    // Etc
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IDynamicArrayAccessor
    {
        [Query]
        IList<DataEntity> QueryData(int[]? ids);
    }

    [Fact]
    public void TestDynamicArray()
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
                .SetSql(
                    "SELECT * FROM Data\r\n" +
                    "/*% if (ids is not null) { */\r\n" +
                    "WHERE Id IN /*@ ids */(2, 4)\r\n" +
                    "/*% } */\r\n")
                .Build();
            var accessor = generator.Create<IDynamicArrayAccessor>();

            var list = accessor.QueryData(null);

            Assert.Equal(4, list.Count);

            list = accessor.QueryData([2, 4]);

            Assert.Equal(2, list.Count);
        }
    }
}
