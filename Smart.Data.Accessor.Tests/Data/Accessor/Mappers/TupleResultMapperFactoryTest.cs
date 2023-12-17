namespace Smart.Data.Accessor.Mappers;

using Smart.Data.Accessor.Engine;
using Smart.Mock.Data;

public sealed class TupleResultMapperFactoryTest
{
    //--------------------------------------------------------------------------------
    // Class/Property
    //--------------------------------------------------------------------------------

    public sealed class ClassPropertyMasterEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public sealed class ClassPropertySlaveEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    [Fact]
    public void TestClassProperty()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name"),
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1", 101, "Slave-101" },
            new object[] { 1, "Master-1", 102, "Slave-102" },
            new object[] { 1, "Master-1", DBNull.Value, DBNull.Value },
            new object[] { 1, "Master-1", DBNull.Value, "-" },
            new object[] { 1, "Master-1", -1, DBNull.Value },
            new object[] { DBNull.Value, DBNull.Value, 201, "Slave-201" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<ClassPropertyMasterEntity, ClassPropertySlaveEntity>>(engine, GetType().GetMethod(nameof(TestClassProperty))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(7, list.Count);

        // All
        var element0 = list[0];
        Assert.Equal(1, element0.Item1.Id);
        Assert.Equal("Master-1", element0.Item1.Name);
        Assert.Equal(101, element0.Item2.Id);
        Assert.Equal("Slave-101", element0.Item2.Name);

        // All2
        var element1 = list[1];
        Assert.Equal(1, element1.Item1.Id);
        Assert.Equal("Master-1", element1.Item1.Name);
        Assert.Equal(102, element1.Item2.Id);
        Assert.Equal("Slave-102", element1.Item2.Name);

        // Slave is NULL
        var element2 = list[2];
        Assert.Equal(1, element2.Item1.Id);
        Assert.Equal("Master-1", element2.Item1.Name);
        Assert.Null(element2.Item2);

        // Slave 1st is NULL
        var element3 = list[3];
        Assert.Equal(1, element3.Item1.Id);
        Assert.Equal("Master-1", element3.Item1.Name);
        Assert.Null(element3.Item2);

        // Slave 1st is not NULL
        var element4 = list[4];
        Assert.Equal(1, element4.Item1.Id);
        Assert.Equal("Master-1", element4.Item1.Name);
        Assert.Equal(-1, element4.Item2.Id);
        Assert.Null(element4.Item2.Name);

        // Master is NULL
        var element5 = list[5];
        Assert.Equal(0, element5.Item1.Id);
        Assert.Null(element5.Item1.Name);
        Assert.Equal(201, element5.Item2.Id);
        Assert.Equal("Slave-201", element5.Item2.Name);

        // All NULL
        var element6 = list[6];
        Assert.Equal(0, element6.Item1.Id);
        Assert.Null(element6.Item1.Name);
        Assert.Null(element6.Item2);
    }

    [Fact]
    public void TestClassPropertyWithConvert()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name"),
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { "1", 10, "2", 20 }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<ClassPropertyMasterEntity, ClassPropertySlaveEntity>>(engine, GetType().GetMethod(nameof(TestClassPropertyWithConvert))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Item1.Id);
        Assert.Equal("10", entity.Item1.Name);
        Assert.Equal(2, entity.Item2.Id);
        Assert.Equal("20", entity.Item2.Name);
    }

    //--------------------------------------------------------------------------------
    // Class/Constructor
    //--------------------------------------------------------------------------------

    public sealed class ClassConstructorMasterEntity
    {
        public int Id { get; }

        public string Name { get; }

        public ClassConstructorMasterEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public sealed class ClassConstructorSlaveEntity
    {
        public int Id { get; }

        public string Name { get; }

        public ClassConstructorSlaveEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void TestClassConstructor()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name"),
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1", 101, "Slave-101" },
            new object[] { 1, "Master-1", 102, "Slave-102" },
            new object[] { 1, "Master-1", DBNull.Value, DBNull.Value },
            new object[] { 1, "Master-1", DBNull.Value, "-" },
            new object[] { 1, "Master-1", -1, DBNull.Value },
            new object[] { DBNull.Value, DBNull.Value, 201, "Slave-201" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<ClassConstructorMasterEntity, ClassConstructorSlaveEntity>>(engine, GetType().GetMethod(nameof(TestClassConstructor))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(7, list.Count);

        // All
        var element0 = list[0];
        Assert.Equal(1, element0.Item1.Id);
        Assert.Equal("Master-1", element0.Item1.Name);
        Assert.Equal(101, element0.Item2.Id);
        Assert.Equal("Slave-101", element0.Item2.Name);

        // All2
        var element1 = list[1];
        Assert.Equal(1, element1.Item1.Id);
        Assert.Equal("Master-1", element1.Item1.Name);
        Assert.Equal(102, element1.Item2.Id);
        Assert.Equal("Slave-102", element1.Item2.Name);

        // Slave is NULL
        var element2 = list[2];
        Assert.Equal(1, element2.Item1.Id);
        Assert.Equal("Master-1", element2.Item1.Name);
        Assert.Null(element2.Item2);

        // Slave 1st is NULL
        var element3 = list[3];
        Assert.Equal(1, element3.Item1.Id);
        Assert.Equal("Master-1", element3.Item1.Name);
        Assert.Null(element3.Item2);

        // Slave 1st is not NULL
        var element4 = list[4];
        Assert.Equal(1, element4.Item1.Id);
        Assert.Equal("Master-1", element4.Item1.Name);
        Assert.Equal(-1, element4.Item2.Id);
        Assert.Null(element4.Item2.Name);

        // Master is NULL
        var element5 = list[5];
        Assert.Equal(0, element5.Item1.Id);
        Assert.Null(element5.Item1.Name);
        Assert.Equal(201, element5.Item2.Id);
        Assert.Equal("Slave-201", element5.Item2.Name);

        // All NULL
        var element6 = list[6];
        Assert.Equal(0, element6.Item1.Id);
        Assert.Null(element6.Item1.Name);
        Assert.Null(element6.Item2);
    }

    [Fact]
    public void TestClassConstructorWithConvert()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name"),
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { "1", 10, "2", 20 }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<ClassConstructorMasterEntity, ClassConstructorSlaveEntity>>(engine, GetType().GetMethod(nameof(TestClassConstructorWithConvert))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Item1.Id);
        Assert.Equal("10", entity.Item1.Name);
        Assert.Equal(2, entity.Item2.Id);
        Assert.Equal("20", entity.Item2.Name);
    }

    //--------------------------------------------------------------------------------
    // Struct/Property
    //--------------------------------------------------------------------------------

    public struct StructPropertyMasterEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public struct StructPropertySlaveEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [Fact]
    public void TestStructProperty()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name"),
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1", 101, "Slave-101" },
            new object[] { 1, "Master-1", 102, "Slave-102" },
            new object[] { 1, "Master-1", DBNull.Value, DBNull.Value },
            new object[] { 1, "Master-1", DBNull.Value, "-" },
            new object[] { 1, "Master-1", -1, DBNull.Value },
            new object[] { DBNull.Value, DBNull.Value, 201, "Slave-201" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<StructPropertyMasterEntity, StructPropertySlaveEntity>>(engine, GetType().GetMethod(nameof(TestStructProperty))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(7, list.Count);

        // All
        var element0 = list[0];
        Assert.Equal(1, element0.Item1.Id);
        Assert.Equal("Master-1", element0.Item1.Name);
        Assert.Equal(101, element0.Item2.Id);
        Assert.Equal("Slave-101", element0.Item2.Name);

        // All2
        var element1 = list[1];
        Assert.Equal(1, element1.Item1.Id);
        Assert.Equal("Master-1", element1.Item1.Name);
        Assert.Equal(102, element1.Item2.Id);
        Assert.Equal("Slave-102", element1.Item2.Name);

        // Slave is NULL
        var element2 = list[2];
        Assert.Equal(1, element2.Item1.Id);
        Assert.Equal("Master-1", element2.Item1.Name);
        Assert.Equal(default, element2.Item2);

        // Slave 1st is NULL
        var element3 = list[3];
        Assert.Equal(1, element3.Item1.Id);
        Assert.Equal("Master-1", element3.Item1.Name);
        Assert.Equal(default, element3.Item2);

        // Slave 1st is not NULL
        var element4 = list[4];
        Assert.Equal(1, element4.Item1.Id);
        Assert.Equal("Master-1", element4.Item1.Name);
        Assert.Equal(-1, element4.Item2.Id);
        Assert.Null(element4.Item2.Name);

        // Master is NULL
        var element5 = list[5];
        Assert.Equal(0, element5.Item1.Id);
        Assert.Null(element5.Item1.Name);
        Assert.Equal(201, element5.Item2.Id);
        Assert.Equal("Slave-201", element5.Item2.Name);

        // All NULL
        var element6 = list[6];
        Assert.Equal(0, element6.Item1.Id);
        Assert.Null(element6.Item1.Name);
        Assert.Equal(default, element6.Item2);
    }

    [Fact]
    public void TestStructPropertyWithConvert()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name"),
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { "1", 10, "2", 20 }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<StructPropertyMasterEntity, StructPropertySlaveEntity>>(engine, GetType().GetMethod(nameof(TestStructPropertyWithConvert))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Item1.Id);
        Assert.Equal("10", entity.Item1.Name);
        Assert.Equal(2, entity.Item2.Id);
        Assert.Equal("20", entity.Item2.Name);
    }

    //--------------------------------------------------------------------------------
    // Struct/Constructor
    //--------------------------------------------------------------------------------

    public readonly struct StructConstructorMasterEntity
    {
        public int Id { get; }

        public string Name { get; }

        public StructConstructorMasterEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public readonly struct StructConstructorSlaveEntity
    {
        public int Id { get; }

        public string Name { get; }

        public StructConstructorSlaveEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    [Fact]
    public void TestStructConstructor()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name"),
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1", 101, "Slave-101" },
            new object[] { 1, "Master-1", 102, "Slave-102" },
            new object[] { 1, "Master-1", DBNull.Value, DBNull.Value },
            new object[] { 1, "Master-1", DBNull.Value, "-" },
            new object[] { 1, "Master-1", -1, DBNull.Value },
            new object[] { DBNull.Value, DBNull.Value, 201, "Slave-201" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<StructConstructorMasterEntity, StructConstructorSlaveEntity>>(engine, GetType().GetMethod(nameof(TestStructConstructor))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(7, list.Count);

        // All
        var element0 = list[0];
        Assert.Equal(1, element0.Item1.Id);
        Assert.Equal("Master-1", element0.Item1.Name);
        Assert.Equal(101, element0.Item2.Id);
        Assert.Equal("Slave-101", element0.Item2.Name);

        // All2
        var element1 = list[1];
        Assert.Equal(1, element1.Item1.Id);
        Assert.Equal("Master-1", element1.Item1.Name);
        Assert.Equal(102, element1.Item2.Id);
        Assert.Equal("Slave-102", element1.Item2.Name);

        // Slave is NULL
        var element2 = list[2];
        Assert.Equal(1, element2.Item1.Id);
        Assert.Equal("Master-1", element2.Item1.Name);
        Assert.Equal(default, element2.Item2);

        // Slave 1st is NULL
        var element3 = list[3];
        Assert.Equal(1, element3.Item1.Id);
        Assert.Equal("Master-1", element3.Item1.Name);
        Assert.Equal(default, element3.Item2);

        // Slave 1st is not NULL
        var element4 = list[4];
        Assert.Equal(1, element4.Item1.Id);
        Assert.Equal("Master-1", element4.Item1.Name);
        Assert.Equal(-1, element4.Item2.Id);
        Assert.Null(element4.Item2.Name);

        // Master is NULL
        var element5 = list[5];
        Assert.Equal(0, element5.Item1.Id);
        Assert.Null(element5.Item1.Name);
        Assert.Equal(201, element5.Item2.Id);
        Assert.Equal("Slave-201", element5.Item2.Name);

        // All NULL
        var element6 = list[6];
        Assert.Equal(0, element6.Item1.Id);
        Assert.Null(element6.Item1.Name);
        Assert.Equal(default, element6.Item2);
    }

    [Fact]
    public void TestStructConstructorWithConvert()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name"),
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { "1", 10, "2", 20 }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<StructConstructorMasterEntity, StructConstructorSlaveEntity>>(engine, GetType().GetMethod(nameof(TestStructConstructorWithConvert))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Item1.Id);
        Assert.Equal("10", entity.Item1.Name);
        Assert.Equal(2, entity.Item2.Id);
        Assert.Equal("20", entity.Item2.Name);
    }

    //--------------------------------------------------------------------------------
    // NullableStruct/Property
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestNullableStructProperty()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name"),
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1", 101, "Slave-101" },
            new object[] { 1, "Master-1", 102, "Slave-102" },
            new object[] { 1, "Master-1", DBNull.Value, DBNull.Value },
            new object[] { 1, "Master-1", DBNull.Value, "-" },
            new object[] { 1, "Master-1", -1, DBNull.Value },
            new object[] { DBNull.Value, DBNull.Value, 201, "Slave-201" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<StructPropertyMasterEntity?, StructPropertySlaveEntity?>>(engine, GetType().GetMethod(nameof(TestNullableStructProperty))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(7, list.Count);

        // All
        var element0 = list[0];
        AssertEx.NotNull(element0.Item1);
        Assert.Equal(1, element0.Item1.Value.Id);
        Assert.Equal("Master-1", element0.Item1.Value.Name);
        AssertEx.NotNull(element0.Item2);
        Assert.Equal(101, element0.Item2.Value.Id);
        Assert.Equal("Slave-101", element0.Item2.Value.Name);

        // All2
        var element1 = list[1];
        AssertEx.NotNull(element1.Item1);
        Assert.Equal(1, element1.Item1.Value.Id);
        Assert.Equal("Master-1", element1.Item1.Value.Name);
        AssertEx.NotNull(element1.Item2);
        Assert.Equal(102, element1.Item2.Value.Id);
        Assert.Equal("Slave-102", element1.Item2.Value.Name);

        // Slave is NULL
        var element2 = list[2];
        AssertEx.NotNull(element2.Item1);
        Assert.Equal(1, element2.Item1.Value.Id);
        Assert.Equal("Master-1", element2.Item1.Value.Name);
        Assert.Null(element2.Item2);

        // Slave 1st is NULL
        var element3 = list[3];
        AssertEx.NotNull(element3.Item1);
        Assert.Equal(1, element3.Item1.Value.Id);
        Assert.Equal("Master-1", element3.Item1.Value.Name);
        Assert.Null(element3.Item2);

        // Slave 1st is not NULL
        var element4 = list[4];
        AssertEx.NotNull(element4.Item1);
        Assert.Equal(1, element4.Item1.Value.Id);
        Assert.Equal("Master-1", element4.Item1.Value.Name);
        AssertEx.NotNull(element4.Item2);
        Assert.Equal(-1, element4.Item2.Value.Id);
        Assert.Null(element4.Item2.Value.Name);

        // Master is NULL
        var element5 = list[5];
        AssertEx.NotNull(element5.Item1);
        Assert.Equal(0, element5.Item1.Value.Id);
        Assert.Null(element5.Item1.Value.Name);
        AssertEx.NotNull(element5.Item2);
        Assert.Equal(201, element5.Item2.Value.Id);
        Assert.Equal("Slave-201", element5.Item2.Value.Name);

        // All NULL
        var element6 = list[6];
        AssertEx.NotNull(element6.Item1);
        Assert.Equal(0, element6.Item1.Value.Id);
        Assert.Null(element6.Item1.Value.Name);
        Assert.Null(element6.Item2);
    }

    //--------------------------------------------------------------------------------
    // NullableStruct/Constructor
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestNullableStructConstructor()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name"),
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1", 101, "Slave-101" },
            new object[] { 1, "Master-1", 102, "Slave-102" },
            new object[] { 1, "Master-1", DBNull.Value, DBNull.Value },
            new object[] { 1, "Master-1", DBNull.Value, "-" },
            new object[] { 1, "Master-1", -1, DBNull.Value },
            new object[] { DBNull.Value, DBNull.Value, 201, "Slave-201" },
            new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<StructConstructorMasterEntity?, StructConstructorSlaveEntity?>>(engine, GetType().GetMethod(nameof(TestNullableStructConstructor))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(7, list.Count);

        // All
        var element0 = list[0];
        AssertEx.NotNull(element0.Item1);
        Assert.Equal(1, element0.Item1.Value.Id);
        Assert.Equal("Master-1", element0.Item1.Value.Name);
        AssertEx.NotNull(element0.Item2);
        Assert.Equal(101, element0.Item2.Value.Id);
        Assert.Equal("Slave-101", element0.Item2.Value.Name);

        // All2
        var element1 = list[1];
        AssertEx.NotNull(element1.Item1);
        Assert.Equal(1, element1.Item1.Value.Id);
        Assert.Equal("Master-1", element1.Item1.Value.Name);
        AssertEx.NotNull(element1.Item2);
        Assert.Equal(102, element1.Item2.Value.Id);
        Assert.Equal("Slave-102", element1.Item2.Value.Name);

        // Slave is NULL
        var element2 = list[2];
        AssertEx.NotNull(element2.Item1);
        Assert.Equal(1, element2.Item1.Value.Id);
        Assert.Equal("Master-1", element2.Item1.Value.Name);
        Assert.Null(element2.Item2);

        // Slave 1st is NULL
        var element3 = list[3];
        AssertEx.NotNull(element3.Item1);
        Assert.Equal(1, element3.Item1.Value.Id);
        Assert.Equal("Master-1", element3.Item1.Value.Name);
        Assert.Null(element3.Item2);

        // Slave 1st is not NULL
        var element4 = list[4];
        AssertEx.NotNull(element4.Item1);
        Assert.Equal(1, element4.Item1.Value.Id);
        Assert.Equal("Master-1", element4.Item1.Value.Name);
        AssertEx.NotNull(element4.Item2);
        Assert.Equal(-1, element4.Item2.Value.Id);
        Assert.Null(element4.Item2.Value.Name);

        // Master is NULL
        var element5 = list[5];
        AssertEx.NotNull(element5.Item1);
        Assert.Equal(0, element5.Item1.Value.Id);
        Assert.Null(element5.Item1.Value.Name);
        AssertEx.NotNull(element5.Item2);
        Assert.Equal(201, element5.Item2.Value.Id);
        Assert.Equal("Slave-201", element5.Item2.Value.Name);

        // All NULL
        var element6 = list[6];
        AssertEx.NotNull(element6.Item1);
        Assert.Equal(0, element6.Item1.Value.Id);
        Assert.Null(element6.Item1.Value.Name);
        Assert.Null(element6.Item2);
    }

    //--------------------------------------------------------------------------------
    // NoMap/Master
    //--------------------------------------------------------------------------------

    public sealed class NoMapMasterMasterEntity
    {
    }

    public sealed class NoMapMasterSlaveEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    [Fact]
    public void TestNoMapMaster()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 101, "Slave-101" },
            new object[] { DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<NoMapMasterMasterEntity, NoMapMasterSlaveEntity>>(engine, GetType().GetMethod(nameof(TestNoMapMaster))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(2, list.Count);

        // All
        var element0 = list[0];
        AssertEx.NotNull(element0.Item1);
        AssertEx.NotNull(element0.Item2);
        Assert.Equal(101, element0.Item2.Id);
        Assert.Equal("Slave-101", element0.Item2.Name);

        // All NULL
        var element1 = list[1];
        AssertEx.NotNull(element1.Item1);
        Assert.Null(element1.Item2);
    }

    //--------------------------------------------------------------------------------
    // NoMap/Slave
    //--------------------------------------------------------------------------------

    public sealed class NoMapSlaveMasterEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    public sealed class NoMapSlaveSlaveEntity
    {
    }

    [Fact]
    public void TestNoMapSlave()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1" },
            new object[] { DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<NoMapSlaveMasterEntity, NoMapSlaveSlaveEntity>>(engine, GetType().GetMethod(nameof(TestNoMapSlave))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(2, list.Count);

        // All
        var element0 = list[0];
        AssertEx.NotNull(element0.Item1);
        Assert.Equal(1, element0.Item1.Id);
        Assert.Equal("Master-1", element0.Item1.Name);
        Assert.Null(element0.Item2);

        // All NULL
        var element1 = list[1];
        AssertEx.NotNull(element1.Item1);
        Assert.Equal(0, element1.Item1.Id);
        Assert.Null(element1.Item1.Name);
        Assert.Null(element1.Item2);
    }

    //--------------------------------------------------------------------------------
    // NoMap/Struct/Slave
    //--------------------------------------------------------------------------------

    public struct NoMapStructSlaveMasterEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    public struct NoMapStructSlaveSlaveEntity
    {
    }

    [Fact]
    public void TestNoMapStructSlave()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1" },
            new object[] { DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<NoMapStructSlaveMasterEntity, NoMapStructSlaveSlaveEntity>>(engine, GetType().GetMethod(nameof(TestNoMapStructSlave))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(2, list.Count);

        // All
        var element0 = list[0];
        Assert.Equal(1, element0.Item1.Id);
        Assert.Equal("Master-1", element0.Item1.Name);
        Assert.Equal(default, element0.Item2);

        // All NULL
        var element1 = list[1];
        Assert.Equal(0, element1.Item1.Id);
        Assert.Null(element1.Item1.Name);
        Assert.Equal(default, element1.Item2);
    }

    //--------------------------------------------------------------------------------
    // NoMap/Struct/Slave
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestNoMapNullableStructSlave()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(int), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "Master-1" },
            new object[] { DBNull.Value, DBNull.Value }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<NoMapStructSlaveMasterEntity?, NoMapStructSlaveSlaveEntity?>>(engine, GetType().GetMethod(nameof(TestNoMapStructSlave))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(2, list.Count);

        // All
        var element0 = list[0];
        AssertEx.NotNull(element0.Item1);
        Assert.Equal(1, element0.Item1.Value.Id);
        Assert.Equal("Master-1", element0.Item1.Value.Name);
        Assert.Null(element0.Item2);

        // All NULL
        var element1 = list[1];
        AssertEx.NotNull(element1.Item1);
        Assert.Equal(0, element1.Item1.Value.Id);
        Assert.Null(element1.Item1.Value.Name);
        Assert.Null(element1.Item2);
    }

    //--------------------------------------------------------------------------------
    // NoMap
    //--------------------------------------------------------------------------------

    public sealed class NoMapMasterEntity
    {
        public int Id { get; }

        public NoMapMasterEntity(int id)
        {
            Id = id;
        }
    }

    public sealed class NoMapSlaveEntity
    {
    }

    [Fact]
    public void TestNoMap()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { "1" }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<Tuple<NoMapMasterEntity, NoMapSlaveEntity>>(engine, GetType().GetMethod(nameof(TestNoMap))!, false);

        Assert.Throws<InvalidOperationException>(() => engine.QueryBuffer(info, cmd));
    }
}
