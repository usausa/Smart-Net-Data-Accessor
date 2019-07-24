namespace Smart.Data.Accessor.Engine
{
    using System.Collections.Generic;
    using System.Linq;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class QueryTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IQuerySimpleDao
        {
            [Query]
            IList<DataEntity> QueryBufferdSimple();

            [Query]
            IEnumerable<DataEntity> QueryNonBufferdSimple();
        }

        [Fact]
        public void QueryBufferdSimple()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQuerySimpleDao>();

                var list = dao.QueryBufferdSimple();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        [Fact]
        public void QueryNonBufferdSimple()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("SELECT * FROM Data ORDER BY Id")
                    .Build();
                var dao = generator.Create<IQuerySimpleDao>();

                var list = dao.QueryNonBufferdSimple().ToList();

                Assert.Equal(2, list.Count);
                Assert.Equal(1, list[0].Id);
                Assert.Equal("Data-1", list[0].Name);
                Assert.Equal(2, list[1].Id);
                Assert.Equal("Data-2", list[1].Name);
            }
        }

        // TODO use sqlite, with con, without con
        // TODO ref mapper
    }
}
