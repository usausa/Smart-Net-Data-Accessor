namespace Smart.Data.Accessor.Builders;

using Smart.Data.Accessor.Attributes;
using Smart.Mock;

public sealed class SelectTest
{
    //--------------------------------------------------------------------------------
    // Order
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectOrderAccessor
    {
        [Select]
        List<MultiKeyEntity> SelectKeyOrder();

        [Select(Order = "Name DESC")]
        List<MultiKeyEntity> SelectCustomOrder();

        [Select]
        List<MultiKeyEntity> SelectParameterOrder([Order] string order);
    }

    [Fact]
    public void TestSelectOrder()
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
            var accessor = generator.Create<ISelectOrderAccessor>();

            var list = accessor.SelectKeyOrder();

            Assert.Equal(3, list.Count);
            Assert.Equal("Data-1", list[0].Name);
            Assert.Equal("Data-2", list[1].Name);
            Assert.Equal("Data-3", list[2].Name);

            list = accessor.SelectCustomOrder();

            Assert.Equal(3, list.Count);
            Assert.Equal("Data-3", list[0].Name);
            Assert.Equal("Data-2", list[1].Name);
            Assert.Equal("Data-1", list[2].Name);

            list = accessor.SelectParameterOrder("Name DESC");

            Assert.Equal(3, list.Count);
            Assert.Equal("Data-3", list[0].Name);
            Assert.Equal("Data-2", list[1].Name);
            Assert.Equal("Data-1", list[2].Name);
        }
    }

    //--------------------------------------------------------------------------------
    // Other
    //--------------------------------------------------------------------------------

    public sealed class OtherEntity
    {
        [Key(1)]
        public long Key1 { get; set; }

        [Key(2)]
        public long Key2 { get; set; }

        public string Name { get; set; } = default!;
    }

    [DataAccessor]
    public interface ISelectOtherAccessor
    {
        [Select(typeof(MultiKeyEntity))]
        List<OtherEntity> SelectByType();

        [Select("MultiKey")]
        List<OtherEntity> SelectByName();
    }

    [Fact]
    public void TestSelectOther()
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
            var accessor = generator.Create<ISelectOtherAccessor>();

            var list = accessor.SelectByType();

            Assert.Equal(3, list.Count);
            Assert.Equal("Data-1", list[0].Name);
            Assert.Equal("Data-2", list[1].Name);
            Assert.Equal("Data-3", list[2].Name);

            list = accessor.SelectByName();

            Assert.Equal(3, list.Count);
            Assert.Equal("Data-1", list[0].Name);
            Assert.Equal("Data-2", list[1].Name);
            Assert.Equal("Data-3", list[2].Name);
        }
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectInvalid1Accessor
    {
        [Select("")]
        List<MultiKeyEntity> Select();
    }

    [DataAccessor]
    public interface ISelectInvalid2Accessor
    {
        [Select]
        void Select();
    }

    [DataAccessor]
    public interface ISelectInvalid3Accessor
    {
        [Select]
        ValueTask Select();
    }

    [Fact]
    public void TestSelectInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(generator.Create<ISelectInvalid1Accessor>);
        Assert.Throws<BuilderException>(generator.Create<ISelectInvalid2Accessor>);
        Assert.Throws<BuilderException>(generator.Create<ISelectInvalid3Accessor>);
    }

    //--------------------------------------------------------------------------------
    // Argument
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectByArgumentAccessor
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
            var accessor = generator.Create<ISelectByArgumentAccessor>();

            var list = accessor.Select(1L, 2L);

            Assert.Equal(2, list.Count);
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
    public interface ISelectByParameterAccessor
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
            var accessor = generator.Create<ISelectByParameterAccessor>();

            var list = accessor.Select(new Parameter { Key1 = 1L, Key2 = 2L });

            Assert.Equal(2, list.Count);
        }
    }

    //--------------------------------------------------------------------------------
    // Exclude
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface ISelectExcludeNullAccessor
    {
        [Select]
        List<MultiKeyEntity> Select([Condition(ExcludeNull = true)] string? type = null);
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
            var accessor = generator.Create<ISelectExcludeNullAccessor>();

            var list = accessor.Select("A");

            Assert.Equal(2, list.Count);

            list = accessor.Select();

            Assert.Equal(3, list.Count);

            list = accessor.Select(string.Empty);

            Assert.Empty(list);
        }
    }

    [DataAccessor]
    public interface ISelectExcludeEmptyAccessor
    {
        [Select]
        List<MultiKeyEntity> Select([Condition(ExcludeEmpty = true)] string? type = null);
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
            var accessor = generator.Create<ISelectExcludeEmptyAccessor>();

            var list = accessor.Select("A");

            Assert.Equal(2, list.Count);

            list = accessor.Select();

            Assert.Equal(3, list.Count);

            list = accessor.Select(string.Empty);

            Assert.Equal(3, list.Count);
        }
    }
}
