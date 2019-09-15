namespace Smart.Data.Accessor.Builders
{
    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class SelectSingleTest
    {
        //--------------------------------------------------------------------------------
        // Key
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISelectByKeyAccessor
        {
            [SelectSingle]
            MultiKeyEntity SelectSingle(MultiKeyEntity entity);
        }

        [Fact]
        public void TestSelectByKey()
        {
            using (TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data-1" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var accessor = generator.Create<ISelectByKeyAccessor>();

                var entity = accessor.SelectSingle(new MultiKeyEntity { Key1 = 1L, Key2 = 2L });

                Assert.NotNull(entity);
            }
        }

        //--------------------------------------------------------------------------------
        // Argument
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISelectByArgumentAccessor
        {
            [SelectSingle(typeof(MultiKeyEntity))]
            MultiKeyEntity SelectByType(long key1, long key2);

            [SelectSingle("MultiKey")]
            MultiKeyEntity SelectByName(long key1, long key2);
        }

        [Fact]
        public void TestSelectByArgument()
        {
            using (TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data-1" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var accessor = generator.Create<ISelectByArgumentAccessor>();

                var entity = accessor.SelectByType(1, 2);

                Assert.NotNull(entity);

                entity = accessor.SelectByName(1, 2);

                Assert.NotNull(entity);
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISelectInvalidAccessor
        {
            [SelectSingle("")]
            MultiKeyEntity SelectSingle();
        }

        [Fact]
        public void TestSelectInvalid()
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();

            Assert.Throws<BuilderException>(() => generator.Create<ISelectInvalidAccessor>());
        }
    }
}
