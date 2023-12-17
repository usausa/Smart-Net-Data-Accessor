namespace Smart.Data.Accessor;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;

public sealed class RawSqlTest
{
    [DataAccessor]
    public interface IRawAccessor
    {
        [Query]
        IList<DataEntity> QueryData(string sort);
    }

    [Fact]
    public void TestReplace()
    {
        using (TestDatabase.Initialize()
            .SetupDataTable()
            .InsertData(new DataEntity { Id = 1, Name = "BBB" })
            .InsertData(new DataEntity { Id = 2, Name = "CCC" })
            .InsertData(new DataEntity { Id = 3, Name = "AAA" }))
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .SetSql("SELECT * FROM Data ORDER BY /*# sort */")
                .Build();
            var accessor = generator.Create<IRawAccessor>();

            var list = accessor.QueryData("Id");

            Assert.Equal(1, list[0].Id);

            list = accessor.QueryData("Id DESC");

            Assert.Equal(3, list[0].Id);

            list = accessor.QueryData("Name DESC");

            Assert.Equal(2, list[0].Id);
        }
    }
}
