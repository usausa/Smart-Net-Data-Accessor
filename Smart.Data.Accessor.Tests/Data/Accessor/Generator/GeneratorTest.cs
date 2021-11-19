namespace Smart.Data.Accessor.Generator;

using System.Data;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;

using Xunit;

public class AccessorGeneratorTest
{
    [DataAccessor]
    public interface IInvalidMethodAccessor
    {
        int Execute();
    }

    [Fact]
    public void TestUnsupportedOperations()
    {
        var generator = new TestFactoryBuilder().Build();

        Assert.Throws<AccessorGeneratorException>(() => generator.Create<object>());

        Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidMethodAccessor>());
    }

    [DataAccessor]
    public interface IInvalidCodeAccessor
    {
        [DirectSql(CommandType.Text, MethodType.Execute, "/*% { */")]
        int Execute();
    }

    [Fact]
    public void TestInvalidCode()
    {
        var generator = new TestFactoryBuilder().Build();

        Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidCodeAccessor>());
    }
}
