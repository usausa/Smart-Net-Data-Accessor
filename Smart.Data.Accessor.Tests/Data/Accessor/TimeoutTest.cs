namespace Smart.Data.Accessor;

using System.Data.Common;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Generator;
using Smart.Mock;
using Smart.Mock.Data;

public sealed class TimeoutTest
{
    [DataAccessor]
    public interface ICommandTimeoutAttributeAccessor
    {
        [Execute]
        [CommandTimeout(123)]
        int Execute(DbConnection con);
    }

    [Fact]
    public void TestCommandTimeoutAttribute()
    {
        var generator = new TestFactoryBuilder()
            .SetSql(string.Empty)
            .Build();

        var accessor = generator.Create<ICommandTimeoutAttributeAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(123, c.CommandTimeout);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con);
    }

    [DataAccessor]
    public interface ITimeoutParameterAccessor
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

        var accessor = generator.Create<ITimeoutParameterAccessor>();

        var con = new MockDbConnection();
        con.SetupCommand(cmd =>
        {
            cmd.Executing = c =>
            {
                Assert.Equal(123, c.CommandTimeout);
            };
            cmd.SetupResult(1);
        });

        accessor.Execute(con, 123);
    }

    [DataAccessor]
    public interface IInvalidTimeoutParameterAccessor
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

        Assert.Throws<AccessorGeneratorException>(generator.Create<IInvalidTimeoutParameterAccessor>);
    }
}
