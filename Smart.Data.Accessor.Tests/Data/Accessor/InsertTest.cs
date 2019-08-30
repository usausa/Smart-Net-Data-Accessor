namespace Smart.Data.Accessor
{
    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class InsertTest
    {
        [DataAccessor]
        public interface IInsertDao
        {
            [Insert]
            int Insert(DataEntity entity);
        }

        [Fact]
        public void TestInsert()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .ConfigureOptions(x =>
                    {
                        x["EntityClassSuffix"] = "Model,Entity";
                    })
                    .Build();
                var dao = generator.Create<IInsertDao>();

                var effect = dao.Insert(new DataEntity { Id = 1, Name = "xxx" });

                Assert.Equal(1, effect);

                var entity = con.QueryData(1);
                Assert.NotNull(entity);
                Assert.Equal(1, entity.Id);
                Assert.Equal("xxx", entity.Name);
            }
        }
    }
}
