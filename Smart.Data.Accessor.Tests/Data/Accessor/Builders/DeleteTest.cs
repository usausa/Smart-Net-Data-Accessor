namespace Smart.Data.Accessor.Builders;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;

public sealed class DeleteTest
{
    //--------------------------------------------------------------------------------
    // All
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IDeleteAllAccessor
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
            var accessor = generator.Create<IDeleteAllAccessor>();

            var effect = accessor.DeleteAll();

            Assert.Equal(3, effect);
        }
    }

    [DataAccessor]
    public interface IDeleteAllWithoutForceAccessor
    {
        [Delete("MultiKey")]
        int DeleteAll();
    }

    [Fact]
    public void TestDeleteAllWithoutForce()
    {
        var generator = new TestFactoryBuilder()
            .Build();
        Assert.Throws<BuilderException>(generator.Create<IDeleteAllWithoutForceAccessor>);
    }

    //--------------------------------------------------------------------------------
    // Key
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IDeleteByKeyAccessor
    {
        [Delete]
        int Delete(MultiKeyEntity entity);
    }

    [Fact]
    public void TestDeleteByKey()
    {
        using var con = TestDatabase.Initialize()
            .SetupMultiKeyTable()
            .InsertMultiKey(new MultiKeyEntity { Key1 = 1, Key2 = 2, Type = "A", Name = "Data-1" });
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<IDeleteByKeyAccessor>();

        var effect = accessor.Delete(new MultiKeyEntity { Key1 = 1L, Key2 = 2L });

        Assert.Equal(1, effect);

        Assert.Null(con.QueryMultiKey(1L, 2L));
    }

    //--------------------------------------------------------------------------------
    // Argument
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IDeleteByArgumentAccessor
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
            var accessor = generator.Create<IDeleteByArgumentAccessor>();

            var effect = accessor.Delete(1L, 2L);

            Assert.Equal(2, effect);
        }
    }

    [DataAccessor]
    public interface IDeleteByArrayArgumentAccessor
    {
        [Count(typeof(MultiKeyEntity))]
        int Delete(long key1, long[] key2);
    }

    [Fact]
    public void TestDeleteByArrayArgument()
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
            var accessor = generator.Create<IDeleteByArrayArgumentAccessor>();

            var effect = accessor.Delete(1L, [1L, 2L]);

            Assert.Equal(2, effect);
        }
    }

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    public sealed class Parameter
    {
        public long Key1 { get; set; }

        [Condition(Operand.GreaterEqualThan)]
        public long Key2 { get; set; }
    }

    [DataAccessor]
    public interface IDeleteByParameterAccessor
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
            var accessor = generator.Create<IDeleteByParameterAccessor>();

            var effect = accessor.Delete(new Parameter { Key1 = 1L, Key2 = 2L });

            Assert.Equal(2, effect);
        }
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IDeleteInvalidAccessor
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

        Assert.Throws<BuilderException>(generator.Create<IDeleteInvalidAccessor>);
    }
}
