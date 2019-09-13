namespace Smart.Data.Accessor
{
    using System.Data.Common;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Generator;
    using Smart.Mock;
    using Smart.Mock.Data;

    using Xunit;

    public class TimeoutTest
    {
        [DataAccessor]
        public interface ITimeoutAttributeDao
        {
            [Execute]
            [CommandTimeout(123)]
            int Execute(DbConnection con);
        }

        [Fact]
        public void TestTimeoutAttribute()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            var dao = generator.Create<ITimeoutAttributeDao>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(123, c.CommandTimeout);
                };
                cmd.SetupResult(1);
            });

            dao.Execute(con);
        }

        [DataAccessor]
        public interface ITimeoutParameterDao
        {
            [Execute]
            int Execute(DbConnection con, [Timeout] int timeout);
        }

        [Fact]
        public void TestTimeoutParameter()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            var dao = generator.Create<ITimeoutParameterDao>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c =>
                {
                    Assert.Equal(123, c.CommandTimeout);
                };
                cmd.SetupResult(1);
            });

            dao.Execute(con, 123);
        }

        [DataAccessor]
        public interface IInvalidTimeoutParameterDao
        {
            [Execute]
            int Execute(DbConnection con, [Timeout] string timeout);
        }

        [Fact]
        public void TestInvalidTimeoutParameter()
        {
            var generator = new TestFactoryBuilder()
                .SetSql(string.Empty)
                .Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidTimeoutParameterDao>());
        }
    }
}
