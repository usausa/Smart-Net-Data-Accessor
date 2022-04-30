namespace Smart.Data.Accessor.Builders;

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
        MultiKeyEntity? SelectSingle(MultiKeyEntity entity);
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

            AssertEx.NotNull(entity);
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

            AssertEx.NotNull(entity);

            entity = accessor.SelectByName(1, 2);

            AssertEx.NotNull(entity);
        }
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectInvalid1Accessor
    {
        [SelectSingle("")]
        MultiKeyEntity SelectSingle();
    }

    [DataAccessor]
    public interface ISelectInvalid2Accessor
    {
        [SelectSingle]
        void SelectSingle();
    }

    [DataAccessor]
    public interface ISelectInvalid3Accessor
    {
        [SelectSingle]
        ValueTask SelectSingle();
    }

    [Fact]
    public void TestSelectInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(() => generator.Create<ISelectInvalid1Accessor>());
        Assert.Throws<BuilderException>(() => generator.Create<ISelectInvalid2Accessor>());
        Assert.Throws<BuilderException>(() => generator.Create<ISelectInvalid3Accessor>());
    }
}
