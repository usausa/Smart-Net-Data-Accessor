namespace Smart.Data.Accessor.Engine
{
    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class ExecuteTest
    {
        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Dao]
        public interface IExecuteSimpleDao
        {
            [Execute]
            int Execute(long id, string name);
        }

        [Fact]
        public void ExecuteSimple()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new GeneratorBuilder()
                    .EnableDebug()
                    .UseFileDatabase()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var dao = generator.Create<IExecuteSimpleDao>();

                var effect = dao.Execute(2, "xxx");

                Assert.Equal(1, effect);

                var entity = con.QueryData(2);
                Assert.NotNull(entity);
                Assert.Equal(2, entity.Id);
                Assert.Equal("xxx", entity.Name);
            }
        }

        // TODO use sqlite, with con, without con
        // TODO ref mapper
    }
}
