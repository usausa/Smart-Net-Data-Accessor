namespace Smart.Data.Accessor.Builders
{
    using Smart.Data.Accessor.Attributes;
    using Smart.Mock;

    using Xunit;

    public class DeleteTest
    {
        //--------------------------------------------------------------------------------
        // All
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IDeleteAllDao
        {
            [Delete(typeof(MultiKeyEntity), Force = true)]
            int DeleteAll();
        }

        [Fact]
        public void TestDeleteAll()
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
                var dao = generator.Create<IDeleteAllDao>();

                var effect = dao.DeleteAll();

                Assert.Equal(3, effect);
            }
        }

        [DataAccessor]
        public interface IDeleteAllWithoutForceDao
        {
            [Delete("MultiKey")]
            int DeleteAll();
        }

        [Fact]
        public void TestDeleteAllWithoutForce()
        {
            var generator = new TestFactoryBuilder()
                .Build();
            Assert.Throws<BuilderException>(() => generator.Create<IDeleteAllWithoutForceDao>());
        }

        //--------------------------------------------------------------------------------
        // Key
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IDeleteByKeyDao
        {
            [Delete]
            int Delete(MultiKeyEntity entity);
        }

        [Fact]
        public void TestDeleteByKey()
        {
            using (var con = TestDatabase.Initialize()
                .SetupMultiKeyTable()
                .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data-1" }))
            {
                var generator = new TestFactoryBuilder()
                    .UseFileDatabase()
                    .Build();
                var dao = generator.Create<IDeleteByKeyDao>();

                var effect = dao.Delete(new MultiKeyEntity { Key1 = 1L, Key2 = 2L });

                Assert.Equal(1, effect);

                Assert.Null(con.QueryMultiKey(1L, 2L));
            }
        }

        //--------------------------------------------------------------------------------
        // Argument
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IDeleteByArgumentDao
        {
            [Count(typeof(MultiKeyEntity))]
            int Delete(long key1, [Condition(Operand.GreaterEqualThan)] long key2);
        }

        [Fact]
        public void TestDeleteByArgument()
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
                var dao = generator.Create<IDeleteByArgumentDao>();

                var effect = dao.Delete(1L, 2L);

                Assert.Equal(2, effect);
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
        public interface IDeleteByParameterDao
        {
            [Count(typeof(MultiKeyEntity))]
            int Delete(Parameter parameter);
        }

        [Fact]
        public void TestDeleteByParameter()
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
                var dao = generator.Create<IDeleteByParameterDao>();

                var effect = dao.Delete(new Parameter { Key1 = 1L, Key2 = 2L });

                Assert.Equal(2, effect);
            }
        }

        //--------------------------------------------------------------------------------
        // Invalid
        //--------------------------------------------------------------------------------

        [DataAccessor]
        public interface IDeleteInvalidDao
        {
            [Delete("")]
            int Delete();
        }

        [Fact]
        public void TestDeleteInvalid()
        {
            var generator = new TestFactoryBuilder()
                .UseFileDatabase()
                .Build();

            Assert.Throws<BuilderException>(() => generator.Create<IDeleteInvalidDao>());
        }
    }
}
