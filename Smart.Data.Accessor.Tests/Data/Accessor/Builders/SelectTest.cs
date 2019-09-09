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
        // All
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISelectAllDao
        {
            [Select]
            List<MultiKeyEntity> SelectAll();
        }

        [Fact]
        public void TestSelectAll()
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
                var dao = generator.Create<ISelectAllDao>();

                var list = dao.SelectAll();

                Assert.Equal(3, list.Count);
            }
        }

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

        //--------------------------------------------------------------------------------
        // Exclude
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface ISelectExcludeNullDao
        {
            [Select]
            List<MultiKeyEntity> Select([Condition(ExcludeNull = true)] string type = null);
        }

        [Fact]
        public void TestSelectExcludeNull()
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
                var dao = generator.Create<ISelectExcludeNullDao>();

                var list = dao.Select("A");

                Assert.Equal(2, list.Count);

                list = dao.Select();

                Assert.Equal(3, list.Count);

                list = dao.Select(string.Empty);

                Assert.Empty(list);
            }
        }

        [DataAccessor]
        public interface ISelectExcludeEmptyDao
        {
            [Select]
            List<MultiKeyEntity> Select([Condition(ExcludeEmpty = true)] string type = null);
        }

        [Fact]
        public void TestSelectExcludeEmpty()
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
                var dao = generator.Create<ISelectExcludeEmptyDao>();

                var list = dao.Select("A");

                Assert.Equal(2, list.Count);

                list = dao.Select();

                Assert.Equal(3, list.Count);

                list = dao.Select(string.Empty);

                Assert.Equal(3, list.Count);
            }
        }
    }
}
