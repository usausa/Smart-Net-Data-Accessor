namespace Smart.Data.Accessor.Builders;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Mapper;
using Smart.Mock;

public sealed class InsertTest
{
    //--------------------------------------------------------------------------------
    // Entity
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertEntityAccessor
    {
        [Insert]
        int Insert(DataEntity entity);
    }

    [Fact]
    public void TestInsertEntity()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<IInsertEntityAccessor>();

        var effect = accessor.Insert(new DataEntity { Id = 1, Name = "xxx" });

        Assert.Equal(1, effect);

        var entity = con.QueryData(1);
        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("xxx", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertParameterAccessor
    {
        [Insert(typeof(DataEntity))]
        int Insert(long id, string name);
    }

    [Fact]
    public void TestInsertParameter()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<IInsertParameterAccessor>();

        var effect = accessor.Insert(1, "xxx");

        Assert.Equal(1, effect);

        var entity = con.QueryData(1);
        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("xxx", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    [DataAccessor]
    public interface IInsertInvalidAccessor
    {
        [Insert]
        int Insert();
    }

    [Fact]
    public void TestInsertInvalid()
    {
        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();

        Assert.Throws<BuilderException>(generator.Create<IInsertInvalidAccessor>);
    }

    //--------------------------------------------------------------------------------
    // Auto Generate
    //--------------------------------------------------------------------------------

    public sealed class AutoGenerateEntity
    {
        [Key]
        public long Id { get; set; }

        [AutoGenerate]
        public string Name { get; set; } = default!;
    }

    [DataAccessor]
    public interface IInsertAutoGenerateAccessor
    {
        [Insert]
        void Insert(AutoGenerateEntity entity);

        [SelectSingle]
        AutoGenerateEntity QueryEntity(long id);
    }

    [Fact]
    public void TestInsertAutoGenerateValue()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        con.Execute("CREATE TABLE IF NOT EXISTS AutoGenerate (Id int, Name text)");

        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<IInsertAutoGenerateAccessor>();

        accessor.Insert(new AutoGenerateEntity { Id = 1, Name = "test" });

        var entity = accessor.QueryEntity(1);

        AssertEx.NotNull(entity);
        Assert.Null(entity.Name);
    }

    //--------------------------------------------------------------------------------
    // DbValue
    //--------------------------------------------------------------------------------

    public sealed class DbValueEntity
    {
        [Key]
        public long Id { get; set; }

        [DbValue("CURRENT_TIMESTAMP")]
        public string DateTime { get; set; } = default!;
    }

    [DataAccessor]
    public interface IInsertDbValueAccessor
    {
        [Insert]
        void Insert(DbValueEntity entity);

        [SelectSingle]
        DbValueEntity QueryEntity(long id);
    }

    [Fact]
    public void TestInsertDbValue()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        con.Execute("CREATE TABLE IF NOT EXISTS DbValue (Id int PRIMARY KEY, DateTime text)");

        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<IInsertDbValueAccessor>();

        accessor.Insert(new DbValueEntity { Id = 1 });

        var entity = accessor.QueryEntity(1);

        AssertEx.NotNull(entity);
        Assert.NotEmpty(entity.DateTime);
    }

    [DataAccessor]
    public interface IInsertAdditionalDbValueAccessor
    {
        [Insert("DbValue")]
        [AdditionalDbValue("DateTime", "CURRENT_TIMESTAMP")]
        void Insert(long id);

        [SelectSingle]
        DbValueEntity QueryEntity(long id);
    }

    [Fact]
    public void TestInsertAdditionalDbValue()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        con.Execute("CREATE TABLE IF NOT EXISTS DbValue (Id int PRIMARY KEY, DateTime text)");

        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .Build();
        var accessor = generator.Create<IInsertAdditionalDbValueAccessor>();

        accessor.Insert(1);

        var entity = accessor.QueryEntity(1);

        AssertEx.NotNull(entity);
        Assert.NotEmpty(entity.DateTime);
    }

    //--------------------------------------------------------------------------------
    // CodeValue
    //--------------------------------------------------------------------------------

    public sealed class Counter
    {
        private long counter;

        public long Next() => ++counter;
    }

    public sealed class CodeValueEntity
    {
        [Key]
        public string Key { get; set; } = default!;

        [CodeValue("counter.Next()")]
        public long Value { get; set; }
    }

    [DataAccessor]
    [Inject(typeof(Counter), "counter")]
    public interface IInsertCodeValueAccessor
    {
        [Insert]
        void Insert(CodeValueEntity entity);

        [SelectSingle]
        CodeValueEntity QueryEntity(string key);
    }

    [Fact]
    public void TestInsertCodeValue()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        con.Execute("CREATE TABLE IF NOT EXISTS CodeValue (Key text PRIMARY KEY, Value int)");

        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .ConfigureComponents(static c => c.Add(new Counter()))
            .Build();
        var accessor = generator.Create<IInsertCodeValueAccessor>();

        accessor.Insert(new CodeValueEntity { Key = "A" });
        accessor.Insert(new CodeValueEntity { Key = "B" });

        var entityA = accessor.QueryEntity("A");
        var entityB = accessor.QueryEntity("B");

        AssertEx.NotNull(entityA);
        Assert.Equal(1, entityA.Value);

        AssertEx.NotNull(entityB);
        Assert.Equal(2, entityB.Value);
    }

    [DataAccessor]
    [Inject(typeof(Counter), "counter")]
    public interface IInsertAdditionalCodeValueAccessor
    {
        [Insert("CodeValue")]
        [AdditionalCodeValue("Value", "counter.Next()")]
        void Insert(string key);

        [SelectSingle]
        CodeValueEntity QueryEntity(string key);
    }

    [Fact]
    public void TestInsertAdditionalCodeValue()
    {
        using var con = TestDatabase.Initialize()
            .SetupDataTable();
        con.Execute("CREATE TABLE IF NOT EXISTS CodeValue (Key text PRIMARY KEY, Value int)");

        var generator = new TestFactoryBuilder()
            .UseFileDatabase()
            .ConfigureComponents(static c => c.Add(new Counter()))
            .Build();
        var accessor = generator.Create<IInsertAdditionalCodeValueAccessor>();

        accessor.Insert("A");
        accessor.Insert("B");

        var entityA = accessor.QueryEntity("A");
        var entityB = accessor.QueryEntity("B");

        AssertEx.NotNull(entityA);
        Assert.Equal(1, entityA.Value);

        AssertEx.NotNull(entityB);
        Assert.Equal(2, entityB.Value);
    }
}
