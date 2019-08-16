namespace Smart.Data.Accessor
{
    using System.Collections.Generic;
    using System.Linq;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class ProviderTest
    {
        [DataAccessor]
        [Provider(ProviderNames.Main)]
        public interface IProviderForExecuteScalarDao
        {
            [ExecuteScalar]
            long Count();

            [ExecuteScalar]
            [Provider(ProviderNames.Sub)]
            long Count2();
        }

        [Fact]
        public void TestProviderForExecuteScalar()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            using (TestDatabase.Initialize2()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseMultipleDatabase()
                    .SetSql("SELECT COUNT(*) FROM Data")
                    .Build();
                var dao = generator.Create<IProviderForExecuteScalarDao>();

                var count = dao.Count();

                Assert.Equal(2, count);

                var count2 = dao.Count2();

                Assert.Equal(1, count2);
            }
        }

        [DataAccessor]
        [Provider(ProviderNames.Main)]
        public interface IProviderForQueryDao
        {
            [Query]
            IEnumerable<DataEntity> Query();

            [Query]
            [Provider(ProviderNames.Sub)]
            IEnumerable<DataEntity> Query2();
        }

        [Fact]
        public void TestProviderForQuery()
        {
            using (TestDatabase.Initialize()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" })
                .InsertData(new DataEntity { Id = 2, Name = "Data-2" }))
            using (TestDatabase.Initialize2()
                .SetupDataTable()
                .InsertData(new DataEntity { Id = 1, Name = "Data-1" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseMultipleDatabase()
                    .SetSql("SELECT * FROM Data")
                    .Build();
                var dao = generator.Create<IProviderForQueryDao>();

                var count = dao.Query().Count();

                Assert.Equal(2, count);

                var count2 = dao.Query2().Count();

                Assert.Equal(1, count2);
            }
        }
    }
}
