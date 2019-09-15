namespace Smart.Data.Accessor.Builders
{
    using System.Data.Common;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;
    using Smart.Mock.Data;

    using Xunit;

    public class TruncateTest
    {
        //--------------------------------------------------------------------------------
        // Truncate
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ITruncateAccessor
        {
            [Truncate(typeof(DataEntity))]
            void TruncateByType(DbConnection con);

            [Truncate("Data")]
            void TruncateByName(DbConnection con);
        }

        [Fact]
        public void TestTruncate()
        {
            var generator = new TestFactoryBuilder()
                .Build();

            var accessor = generator.Create<ITruncateAccessor>();

            var con = new MockDbConnection();
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal("TRUNCATE TABLE Data", c.CommandText);
                cmd.SetupResult(0);
            });
            con.SetupCommand(cmd =>
            {
                cmd.Executing = c => Assert.Equal("TRUNCATE TABLE Data", c.CommandText);
                cmd.SetupResult(0);
            });

            accessor.TruncateByType(con);

            accessor.TruncateByName(con);
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ITruncateInvalidAccessor
        {
            [Truncate("")]
            void Truncate(DbConnection con);
        }

        [Fact]
        public void TestTruncateInvalid()
        {
            var generator = new TestFactoryBuilder()
                .Build();

            Assert.Throws<BuilderException>(() => generator.Create<ITruncateInvalidAccessor>());
        }
    }
}
