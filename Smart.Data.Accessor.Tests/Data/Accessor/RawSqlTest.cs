namespace Smart.Data.Accessor
{
    using System.Collections.Generic;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class RawSqlTest
    {
        [DataAccessor]
        public interface IRawDao
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
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY /*# sort */")
                    .Build();
                var dao = generator.Create<IRawDao>();

                var list = dao.QueryData("Id");

                Assert.Equal(1, list[0].Id);

                list = dao.QueryData("Id DESC");

                Assert.Equal(3, list[0].Id);

                list = dao.QueryData("Name DESC");

                Assert.Equal(2, list[0].Id);
            }
        }
    }
}
