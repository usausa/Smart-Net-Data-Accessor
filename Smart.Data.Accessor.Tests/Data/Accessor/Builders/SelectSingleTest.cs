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
        public interface ISelectByKeyDao
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
                var dao = generator.Create<ISelectByKeyDao>();

                var entity = dao.SelectSingle(new MultiKeyEntity { Key1 = 1L, Key2 = 2L });

                Assert.NotNull(entity);
            }
        }

        //--------------------------------------------------------------------------------
        // Argument
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISelectByArgumentDao
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
                var dao = generator.Create<ISelectByArgumentDao>();

                var entity = dao.SelectByType(1, 2);

                Assert.NotNull(entity);

                entity = dao.SelectByName(1, 2);

                Assert.NotNull(entity);
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISelectInvalidDao
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

            Assert.Throws<BuilderException>(() => generator.Create<ISelectInvalidDao>());
        }
    }
}
