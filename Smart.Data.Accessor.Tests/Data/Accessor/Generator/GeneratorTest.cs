namespace Smart.Data.Accessor.Generator
{
    using System.Data;

    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class DaoGeneratorTest
    {
        [DataAccessor]
        public interface IInvalidMethodDao
        {
            int Execute();
        }

        [Fact]
        public void TestUnsupportedOperations()
        {
            var generator = new TestFactoryBuilder().Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<object>());

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidMethodDao>());
        }

        [DataAccessor]
        public interface IInvalidCodeDao
        {
            [DirectSql(CommandType.Text, MethodType.Execute, "/*% { */")]
            int Execute();
        }

        [Fact]
        public void TestInvalidCode()
        {
            var generator = new TestFactoryBuilder().Build();

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidCodeDao>());
        }
    }
}
