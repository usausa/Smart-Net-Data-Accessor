namespace Smart.Data.Accessor.Engine
{
    using System.Data;
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class ExecuteReaderTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IExecuteReaderSimpleDao
        {
            [ExecuteReader]
            IDataReader QueryBufferd();
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
                var dao = generator.Create<IExecuteReaderSimpleDao>();

                using (var reader = dao.QueryBufferd())
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.Read());
                    Assert.False(reader.Read());
                }
            }
        }

        [Dao]
        public interface IExecuteReaderSimpleAsyncDao
        {
            [ExecuteReader]
            Task<IDataReader> QueryBufferdAsync();
        }

        [Fact]
        public async Task QueryBufferdSimpleAsync()
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
                var dao = generator.Create<IExecuteReaderSimpleAsyncDao>();

                using (var reader = await dao.QueryBufferdAsync())
                {
                    Assert.True(reader.Read());
                    Assert.True(reader.Read());
                    Assert.False(reader.Read());
                }
            }
        }

        // TODO use sqlite, with con, without con
        // TODO ref mapper
    }
}
