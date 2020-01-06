namespace Smart.Data.Accessor
{
    using System.Data.Common;
    using System.Threading.Tasks;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class TransactionTest
    {
        [DataAccessor]
        public interface ITransactionAccessor
        {
            [Execute]
            int Execute(DbTransaction tx, long id, string name);
        }

        [Fact]
        public void TestTransaction()
        {
            using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var accessor = generator.Create<ITransactionAccessor>();

                con.Open();

                using (var tx = con.BeginTransaction())
                {
                    var effect = accessor.Execute(tx, 1L, "xxx");
                    Assert.Equal(1, effect);

                    tx.Rollback();
                }

                var entity = con.QueryData(1L);
                Assert.Null(entity);

                using (var tx = con.BeginTransaction())
                {
                    var effect = accessor.Execute(tx, 1L, "xxx");
                    Assert.Equal(1, effect);

                    tx.Commit();
                }

                entity = con.QueryData(1L);
                Assert.NotNull(entity);
            }
        }

        [DataAccessor]
        public interface ITransactionAsyncAccessor
        {
            [Execute]
            ValueTask<int> ExecuteAsync(DbTransaction tx, long id, string name);
        }

        [Fact]
        public async ValueTask TestTransactionAsync()
        {
            await using (var con = TestDatabase.Initialize()
                .SetupDataTable())
            {
                var generator = new TestFactoryBuilder()
                    .SetSql("INSERT INTO Data (Id, Name) VALUES (/*@ id */1, /*@ name */'test')")
                    .Build();
                var accessor = generator.Create<ITransactionAsyncAccessor>();

                con.Open();

                await using (var tx = con.BeginTransaction())
                {
                    var effect = await accessor.ExecuteAsync(tx, 1L, "xxx");
                    Assert.Equal(1, effect);

                    tx.Rollback();
                }

                var entity = con.QueryData(1L);
                Assert.Null(entity);

                await using (var tx = con.BeginTransaction())
                {
                    var effect = await accessor.ExecuteAsync(tx, 1L, "xxx");
                    Assert.Equal(1, effect);

                    tx.Commit();
                }

                entity = con.QueryData(1L);
                Assert.NotNull(entity);
            }
        }
    }
}
