namespace Smart.Data.Accessor.Builders
{
    using System.Collections.Generic;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Attributes.Builders;
    using Smart.Mock;

    using Xunit;

    public class SelectTest
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
            [Select]
            List<MultiKeyEntity> Select(long key1, [Condition(Operand.GreaterEqualThan)] long key2);
        }

        [Fact]
        public void TestSelectByArgument()
        {
            using (TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<ISelectByArgumentDao>();

                var list = dao.Select(1L, 2L);

                Assert.Equal(2, list.Count);
            }
        }

        //--------------------------------------------------------------------------------
        // Parameter
        //--------------------------------------------------------------------------------

        public class Parameter
        {
            public long Key1 { get; set; }

            [Condition(Operand.GreaterEqualThan)]
            public long Key2 { get; set; }
        }

        [DataAccessor]
        public interface ISelectByParameterDao
        {
            [Select]
            List<MultiKeyEntity> Select(Parameter parameter);
        }

        [Fact]
        public void TestSelectByParameter()
        {
            using (TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 1, Type = "A", Name = "Data-1" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "B", Name = "Data-2" })
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 3, Type = "A", Name = "Data-3" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<ISelectByParameterDao>();

                var list = dao.Select(new Parameter { Key1 = 1L, Key2 = 2L });

                Assert.Equal(2, list.Count);
            }
        }
    }
}
