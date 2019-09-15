namespace Smart.Data.Accessor
{
    using System;
    using System.Data.Common;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;
    using Smart.Mock.Data;

    using Xunit;

    public class SqlSizeTest
    {
        [DataAccessor]
        public interface IInvalidSqlSizeAccessor
        {
            [Execute]
            [SqlSize(-1)]
            void Execute(DbConnection con);
        }

        [Fact]
        public void TestInvalidSqlSize()
        {
            var generator = new TestFactoryBuilder()
                .SetSql("SELECT * FROM Data WHERE /*% // dummy */")
                .Build();

            var accessor = generator.Create<IInvalidSqlSizeAccessor>();

            Assert.Throws<ArgumentOutOfRangeException>(() => accessor.Execute(new MockDbConnection()));
        }
    }
}
