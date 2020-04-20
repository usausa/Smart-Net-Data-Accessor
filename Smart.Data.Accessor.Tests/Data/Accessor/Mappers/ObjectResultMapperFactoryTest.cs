namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Engine;
    using Smart.Mock;
    using Smart.Mock.Data;

    using Xunit;

    public class ObjectResultMapperFactoryTest
    {
        //--------------------------------------------------------------------------------
        // Map
        //--------------------------------------------------------------------------------

        public enum Value
        {
            Zero = 0,
            One = 1
        }

        public class MapEntity
        {
            public int Column1 { get; set; }

            public int? Column2 { get; set; }

            public long Column3 { get; set; }

            public Value Column4 { get; set; }

            public Value? Column5 { get; set; }

            public int Column6 => Column7;

            [Ignore]
            public int Column7 { get; set; }
        }

        [Fact]
        public void TestMapProperty()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(int), "Column1"),
                new MockColumn(typeof(int), "Column2"),
                new MockColumn(typeof(int), "Column3"),
                new MockColumn(typeof(int), "Column4"),
                new MockColumn(typeof(int), "Column5"),
                new MockColumn(typeof(int), "Column6"),
                new MockColumn(typeof(int), "Column7"),
                new MockColumn(typeof(int), "Column8")
            };
            var values = new List<object[]>
            {
                new object[] { 1, 1, 1, 1, 1, 1, 1, 1 },
                new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<MapEntity>(engine, GetType().GetMethod(nameof(TestMapProperty)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0].Column1);
            Assert.Equal(1, list[0].Column2);
            Assert.Equal(1, list[0].Column3);
            Assert.Equal(Value.One, list[0].Column4);
            Assert.Equal(Value.One, list[0].Column5);
            Assert.Equal(0, list[0].Column6);
            Assert.Equal(0, list[0].Column7);

            Assert.Equal(0, list[1].Column1);
            Assert.Null(list[1].Column2);
            Assert.Equal(0, list[1].Column3);
            Assert.Equal(Value.Zero, list[1].Column4);
            Assert.Null(list[1].Column5);
            Assert.Equal(0, list[1].Column6);
            Assert.Equal(0, list[1].Column7);
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

        public class ParserEntity
        {
            [CustomParser]
            public long Id { get; set; }

            [CustomParser]
            public string Name { get; set; }
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

            var info = new QueryInfo<ParserEntity>(engine, GetType().GetMethod(nameof(TestCustomParser)), false);

            var entity = engine.QueryFirstOrDefault(info, cmd);

            Assert.NotNull(entity);
            Assert.Equal(1, entity.Id);
            Assert.Equal("2", entity.Name);
        }

        //--------------------------------------------------------------------------------
        // Spec
        //--------------------------------------------------------------------------------

        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        public class ByConstructor
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public ByConstructor(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }
        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper restore MemberCanBePrivate.Global

        [Fact]
        public void TestByConstructor()
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

            var info = new QueryInfo<ByConstructor>(engine, GetType().GetMethod(nameof(TestMapProperty)), false);

            var entity = engine.QueryFirstOrDefault(info, cmd);

            Assert.NotNull(entity);
            Assert.Equal(1, entity.Id);
            Assert.Equal("2", entity.Name);
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

            var info = new QueryInfo<DataEntity>(engine, null, false);

            Assert.Throws<AccessorRuntimeException>(() => engine.QueryBuffer(info, cmd));
        }
    }
}
