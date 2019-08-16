namespace Smart.Data.Accessor.Generator
{
    using System;
    using System.Data;

    using Smart.Data.Accessor.Attributes;

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
            var generator = new DaoGenerator(null, null);

            Assert.Throws<ArgumentNullException>(() => generator.Create(null));

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<object>());

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidMethodDao>());
        }

        [DataAccessor]
        public interface IInvalidCodeDao
        {
            [DirectSql(CommandType.Text, MethodType.Execute, "/*% { */")]
            int Execute();
        }

        private class RecordDebugger : IGeneratorDebugger
        {
            public BuildError[] Errors { get; private set; }

            public void Log(bool success, DaoSource source, BuildError[] errors)
            {
                Errors = errors;
            }
        }

        [Fact]
        public void TestInvalidCode()
        {
            var generator = new DaoGenerator(null, null);
            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidCodeDao>());

            var debugger = new RecordDebugger();
            generator = new DaoGenerator(null, null, debugger);

            Assert.Throws<AccessorGeneratorException>(() => generator.Create<IInvalidCodeDao>());
            Assert.True(debugger.Errors.Length > 0);
        }
    }
}
