namespace Smart.Data.Accessor.Mappers;

using System.Globalization;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Engine;
using Smart.Mock;
using Smart.Mock.Data;

public sealed class ObjectResultMapperFactoryTest
{
    //--------------------------------------------------------------------------------
    // Map
    //--------------------------------------------------------------------------------

    public enum Value
    {
        Zero = 0,
        One = 1
    }

    public sealed class MapEntity
    {
        public bool BoolColumn { get; set; }

        public bool? BoolColumnN { get; set; }

        public byte ByteColumn { get; set; }

        public byte? ByteColumnN { get; set; }

        public char CharColumn { get; set; }

        public char? CharColumnN { get; set; }

        public short ShortColumn { get; set; }

        public short? ShortColumnN { get; set; }

        public int IntColumn { get; set; }

        public int? IntColumnN { get; set; }

        public long LongColumn { get; set; }

        public long? LongColumnN { get; set; }

        public float FloatColumn { get; set; }

        public float? FloatColumnN { get; set; }

        public double DoubleColumn { get; set; }

        public double? DoubleColumnN { get; set; }

        // Supported struct

        public decimal DecimalColumn { get; set; }

        public decimal? DecimalColumnN { get; set; }

        public DateTime DateTimeColumn { get; set; }

        public DateTime? DateTimeColumnN { get; set; }

        public Guid GuidColumn { get; set; }

        public Guid? GuidColumnN { get; set; }

        // Other primitive

        public sbyte SByteColumn { get; set; }

        public sbyte? SByteColumnN { get; set; }

        public ushort UShortColumn { get; set; }

        public ushort? UShortColumnN { get; set; }

        public uint UIntColumn { get; set; }

        public uint? UIntColumnN { get; set; }

        public ulong ULongColumn { get; set; }

        public ulong? ULongColumnN { get; set; }

        public IntPtr IntPtrColumn { get; set; }

        public IntPtr? IntPtrColumnN { get; set; }

        public UIntPtr UIntPtrColumn { get; set; }

        public UIntPtr? UIntPtrColumnN { get; set; }

        // Enum

        public Value EnumColumn { get; set; }

        public Value? EnumColumnN { get; set; }

        public int ReadonlyColumn => IgnoreColumn;

        [Ignore]
        public int IgnoreColumn { get; set; }
    }

    [Fact]
    public void TestMapProperty()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(bool), nameof(MapEntity.BoolColumn)),
            new MockColumn(typeof(bool), nameof(MapEntity.BoolColumnN)),
            new MockColumn(typeof(byte), nameof(MapEntity.ByteColumn)),
            new MockColumn(typeof(byte), nameof(MapEntity.ByteColumnN)),
            new MockColumn(typeof(char), nameof(MapEntity.CharColumn)),
            new MockColumn(typeof(char), nameof(MapEntity.CharColumnN)),
            new MockColumn(typeof(short), nameof(MapEntity.ShortColumn)),
            new MockColumn(typeof(short), nameof(MapEntity.ShortColumnN)),
            new MockColumn(typeof(int), nameof(MapEntity.IntColumn)),
            new MockColumn(typeof(int), nameof(MapEntity.IntColumnN)),
            new MockColumn(typeof(long), nameof(MapEntity.LongColumn)),
            new MockColumn(typeof(long), nameof(MapEntity.LongColumnN)),
            new MockColumn(typeof(float), nameof(MapEntity.FloatColumn)),
            new MockColumn(typeof(float), nameof(MapEntity.FloatColumnN)),
            new MockColumn(typeof(double), nameof(MapEntity.DoubleColumn)),
            new MockColumn(typeof(double), nameof(MapEntity.DoubleColumnN)),
            new MockColumn(typeof(decimal), nameof(MapEntity.DecimalColumn)),
            new MockColumn(typeof(decimal), nameof(MapEntity.DecimalColumnN)),
            new MockColumn(typeof(DateTime), nameof(MapEntity.DateTimeColumn)),
            new MockColumn(typeof(DateTime), nameof(MapEntity.DateTimeColumnN)),
            new MockColumn(typeof(Guid), nameof(MapEntity.GuidColumn)),
            new MockColumn(typeof(Guid), nameof(MapEntity.GuidColumnN)),
            new MockColumn(typeof(byte), nameof(MapEntity.SByteColumn)),
            new MockColumn(typeof(byte), nameof(MapEntity.SByteColumnN)),
            new MockColumn(typeof(short), nameof(MapEntity.UShortColumn)),
            new MockColumn(typeof(short), nameof(MapEntity.UShortColumnN)),
            new MockColumn(typeof(int), nameof(MapEntity.UIntColumn)),
            new MockColumn(typeof(int), nameof(MapEntity.UIntColumnN)),
            new MockColumn(typeof(long), nameof(MapEntity.ULongColumn)),
            new MockColumn(typeof(long), nameof(MapEntity.ULongColumnN)),
            new MockColumn(typeof(int), nameof(MapEntity.IntPtrColumn)),
            new MockColumn(typeof(int), nameof(MapEntity.IntPtrColumnN)),
            new MockColumn(typeof(int), nameof(MapEntity.UIntPtrColumn)),
            new MockColumn(typeof(int), nameof(MapEntity.UIntPtrColumnN)),
            new MockColumn(typeof(int), nameof(MapEntity.EnumColumn)),
            new MockColumn(typeof(int), nameof(MapEntity.EnumColumnN)),
            new MockColumn(typeof(int), nameof(MapEntity.ReadonlyColumn)),
            new MockColumn(typeof(int), nameof(MapEntity.IgnoreColumn))
        };
        var values = new List<object[]>
        {
            new object[]
            {
                true,
                true,
                (byte)1,
                (byte)1,
                '1',
                '1',
                (short)1,
                (short)1,
                1,
                1,
                1L,
                1L,
                1f,
                1f,
                1d,
                1d,
                1m,
                1m,
                new DateTime(2000, 1, 1),
                new DateTime(2000, 1, 1),
                Guid.Parse("11111111111111111111111111111111"),
                Guid.Parse("11111111111111111111111111111111"),
                (byte)1,
                (byte)1,
                (short)1,
                (short)1,
                1,
                1,
                1L,
                1L,
                1,
                1,
                1,
                1,
                1,
                1,
                1,
                1
            },
            Enumerable.Range(1, columns.Length).Select(static _ => (object)DBNull.Value).ToArray()
        };

        // Execute
        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<MapEntity>(engine, GetType().GetMethod(nameof(TestMapProperty))!, false);

        var list = engine.QueryBuffer(info, cmd);

        Assert.Equal(2, list.Count);

        // Assert values
        Assert.True(list[0].BoolColumn);
        Assert.True(list[0].BoolColumnN);
        Assert.Equal((byte)1, list[0].ByteColumn);
        Assert.Equal((byte)1, list[0].ByteColumnN);
        Assert.Equal('1', list[0].CharColumn);
        Assert.Equal('1', list[0].CharColumnN);
        Assert.Equal((short)1, list[0].ShortColumn);
        Assert.Equal((short)1, list[0].ShortColumnN);
        Assert.Equal(1, list[0].IntColumn);
        Assert.Equal(1, list[0].IntColumnN);
        Assert.Equal(1L, list[0].LongColumn);
        Assert.Equal(1L, list[0].LongColumnN);
        Assert.Equal(1f, list[0].FloatColumn);
        Assert.Equal(1f, list[0].FloatColumnN);
        Assert.Equal(1d, list[0].DoubleColumn);
        Assert.Equal(1d, list[0].DoubleColumnN);

        Assert.Equal(1m, list[0].DecimalColumn);
        Assert.Equal(1m, list[0].DecimalColumnN);
        Assert.Equal(new DateTime(2000, 1, 1), list[0].DateTimeColumn);
        Assert.Equal(new DateTime(2000, 1, 1), list[0].DateTimeColumnN);
        Assert.Equal(Guid.Parse("11111111111111111111111111111111"), list[0].GuidColumn);
        Assert.Equal(Guid.Parse("11111111111111111111111111111111"), list[0].GuidColumnN);

        Assert.Equal((sbyte)1, list[0].SByteColumn);
        Assert.Equal((sbyte)1, list[0].SByteColumnN);
        Assert.Equal((ushort)1, list[0].UShortColumn);
        Assert.Equal((ushort)1, list[0].UShortColumnN);
        Assert.Equal(1u, list[0].UIntColumn);
        Assert.Equal(1u, list[0].UIntColumnN);
        Assert.Equal(1ul, list[0].ULongColumn);
        Assert.Equal(1ul, list[0].ULongColumnN);
        Assert.Equal(1, list[0].IntPtrColumn);
        Assert.Equal(1, list[0].IntPtrColumnN);
        Assert.Equal((UIntPtr)1, list[0].UIntPtrColumn);
        Assert.Equal((UIntPtr)1, list[0].UIntPtrColumnN);

        Assert.Equal(Value.One, list[0].EnumColumn);
        Assert.Equal(Value.One, list[0].EnumColumnN);

        Assert.Equal(0, list[0].ReadonlyColumn);
        Assert.Equal(0, list[0].IgnoreColumn);

        // Assert null
        Assert.Equal(default, list[1].BoolColumn);
        Assert.Null(list[1].BoolColumnN);
        Assert.Equal(default, list[1].ByteColumn);
        Assert.Null(list[1].ByteColumnN);
        Assert.Equal(default, list[1].CharColumn);
        Assert.Null(list[1].CharColumnN);
        Assert.Equal(default, list[1].ShortColumn);
        Assert.Null(list[1].ShortColumnN);
        Assert.Equal(default, list[1].IntColumn);
        Assert.Null(list[1].IntColumnN);
        Assert.Equal(default, list[1].LongColumn);
        Assert.Null(list[1].LongColumnN);
        Assert.Equal(default, list[1].FloatColumn);
        Assert.Null(list[1].FloatColumnN);
        Assert.Equal(default, list[1].DoubleColumn);
        Assert.Null(list[1].DoubleColumnN);

        Assert.Equal(default, list[1].DecimalColumn);
        Assert.Null(list[1].DecimalColumnN);
        Assert.Equal(default, list[1].DateTimeColumn);
        Assert.Null(list[1].DateTimeColumnN);
        Assert.Equal(default, list[1].GuidColumn);
        Assert.Null(list[1].GuidColumnN);

        Assert.Equal(default, list[1].SByteColumn);
        Assert.Null(list[1].SByteColumnN);
        Assert.Equal(default, list[1].UShortColumn);
        Assert.Null(list[1].UShortColumnN);
        Assert.Equal(default, list[1].UIntColumn);
        Assert.Null(list[1].UIntColumnN);
        Assert.Equal(default, list[1].ULongColumn);
        Assert.Null(list[1].ULongColumnN);
        Assert.Equal(default, list[1].IntPtrColumn);
        Assert.Null(list[1].IntPtrColumnN);
        Assert.Equal(default, list[1].UIntPtrColumn);
        Assert.Null(list[1].UIntPtrColumnN);

        Assert.Equal(default, list[1].EnumColumn);
        Assert.Null(list[1].EnumColumnN);

        Assert.Equal(0, list[1].ReadonlyColumn);
        Assert.Equal(0, list[1].IgnoreColumn);
    }

    //--------------------------------------------------------------------------------
    // Parser
    //--------------------------------------------------------------------------------

    public sealed class CustomParserAttribute : ResultParserAttribute
    {
        public override Func<object, object> CreateParser(IServiceProvider serviceProvider, Type type)
        {
            return x => Convert.ChangeType(x, type, CultureInfo.InvariantCulture);
        }
    }

    public sealed class ParserEntity
    {
        [CustomParser]
        public long Id { get; set; }

        [CustomParser]
        public string Name { get; set; } = default!;
    }

    [Fact]
    public void TestCustomParser()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { "1", 2 }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<ParserEntity>(engine, GetType().GetMethod(nameof(TestCustomParser))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("2", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // Struct
    //--------------------------------------------------------------------------------

    public struct StructEntity
    {
        public int Id { get; set; }

        public string Name { get; set; }
    }

    [Fact]
    public void TestStruct()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "2" }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<StructEntity>(engine, GetType().GetMethod(nameof(TestStruct))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        Assert.Equal(1, entity.Id);
        Assert.Equal("2", entity.Name);
    }

    [Fact]
    public void TestStructNullable()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "2" }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));
        cmd.SetupResult(new MockDataReader(columns, new List<object[]>()));

        var info = new QueryInfo<StructEntity?>(engine, GetType().GetMethod(nameof(TestStructNullable))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Value.Id);
        Assert.Equal("2", entity.Value.Name);

        entity = engine.QueryFirstOrDefault(info, cmd);

        Assert.False(entity.HasValue);
    }

    //--------------------------------------------------------------------------------
    // Record
    //--------------------------------------------------------------------------------

    public record RecordEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
    }

    [Fact]
    public void TestRecord()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "2" }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<RecordEntity>(engine, GetType().GetMethod(nameof(TestRecord))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("2", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // InitOnly
    //--------------------------------------------------------------------------------

    public sealed class InitOnlyEntity
    {
        public int Id { get; init; }

        public string Name { get; init; } = default!;
    }

    [Fact]
    public void TestInitOnly()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "2" }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<InitOnlyEntity>(engine, GetType().GetMethod(nameof(TestInitOnly))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("2", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public sealed class ClassConstructorEntity
    {
        public int Id { get; }

        public string Name { get; }

        public ClassConstructorEntity(int id, string name)
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
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "2" }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<ClassConstructorEntity>(engine, GetType().GetMethod(nameof(TestClassConstructor))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("2", entity.Name);
    }

    [Fact]
    public void TestClassConstructorWithConvert()
    {
        var engine = new ExecuteEngineConfig().ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(string), "Id"),
            new MockColumn(typeof(int), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { "1", 2 }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<ClassConstructorEntity>(engine, GetType().GetMethod(nameof(TestClassConstructorWithConvert))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        AssertEx.NotNull(entity);
        Assert.Equal(1, entity.Id);
        Assert.Equal("2", entity.Name);
    }

    public readonly struct StructConstructorEntity
    {
        public int Id { get; }

        public string Name { get; }

        public StructConstructorEntity(int id, string name)
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
            new MockColumn(typeof(string), "Name")
        };
        var values = new List<object[]>
        {
            new object[] { 1, "2" }
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, values));

        var info = new QueryInfo<StructConstructorEntity>(engine, GetType().GetMethod(nameof(TestStructConstructor))!, false);

        var entity = engine.QueryFirstOrDefault(info, cmd);

        Assert.Equal(1, entity.Id);
        Assert.Equal("2", entity.Name);
    }

    //--------------------------------------------------------------------------------
    // Invalid
    //--------------------------------------------------------------------------------

    public sealed class NoMapEntity
    {
        public int Id { get; }

        public NoMapEntity(int id)
        {
            Id = id;
        }
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

        var info = new QueryInfo<NoMapEntity>(engine, GetType().GetMethod(nameof(TestCustomParser))!, false);

        Assert.Throws<InvalidOperationException>(() => engine.QueryBuffer(info, cmd));
    }

    //--------------------------------------------------------------------------------
    // No factory
    //--------------------------------------------------------------------------------

    [Fact]
    public void TestFactoryNotExists()
    {
        var engine = new ExecuteEngineConfig()
            .ConfigureResultMapperFactories(mappers => mappers.Clear())
            .ToEngine();

        var columns = new[]
        {
            new MockColumn(typeof(long), "Id"),
            new MockColumn(typeof(string), "Name")
        };

        var cmd = new MockDbCommand();
        cmd.SetupResult(new MockDataReader(columns, new List<object[]>()));

        var info = new QueryInfo<DataEntity>(engine, null!, false);

        Assert.Throws<AccessorRuntimeException>(() => engine.QueryBuffer(info, cmd));
    }
}
