namespace Smart.Data.Accessor
{
    using System.Data;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class DirectSqlTest
    {
        [Dao]
        public interface IDirectSqlDao
        {
            [DirectSql(CommandType.Text, MethodType.ExecuteScalar, "SELECT COUNT(*) FROM Data")]
            long Count();
        }

        [Fact]
        public void TestDirectSql()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<IDirectSqlDao>();

                var count = dao.Count();

                Assert.Equal(2, count);
            }
        }
    }
}
