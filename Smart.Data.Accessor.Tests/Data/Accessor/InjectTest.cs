namespace Smart.Data.Accessor;

using System.Data;
using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;
using Smart.Mock.Data;

public sealed class InjectTest
{
    public sealed class Counter
    {
        private int counter;

        public int Next() => ++counter;
    }

    [DataAccessor]
    [Inject(typeof(Counter), "counter")]
    public interface IInjectAccessor
    {
        [Execute]
        void Execute(DbConnection con);
    }

    [Fact]
    public void TestInsert()
    {
        var generator = new TestFactoryBuilder()
            .SetSql("/*@ counter.Next() */0, /*@ counter.Next() */0")
            .Config(config => config.ConfigureComponents(c => c.Add<Counter>()))
            .Build();
        var accessor = generator.Create<IInjectAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(static cmd =>
        {
            cmd.Executing = static c =>
            {
                Assert.Equal(DbType.Int32, c.Parameters[0].DbType);
                Assert.Equal(1, c.Parameters[0].Value);
                Assert.Equal(DbType.Int32, c.Parameters[1].DbType);
                Assert.Equal(2, c.Parameters[1].Value);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con);
    }
}
