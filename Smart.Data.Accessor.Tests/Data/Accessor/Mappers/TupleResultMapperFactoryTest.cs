namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;

    using Smart.Data.Accessor.Engine;
    using Smart.Mock.Data;

    using Xunit;

    public class TupleResultMapperFactoryTest
    {
        //--------------------------------------------------------------------------------
        // Class/Property
        //--------------------------------------------------------------------------------

        public class ClassPropertyMasterEntity
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class ClassPropertySlaveEntity
        {
            public int Id { get; set; }

            public string Name { get; set; }
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

            var info = new QueryInfo<Tuple<ClassPropertyMasterEntity, ClassPropertySlaveEntity>>(engine, GetType().GetMethod(nameof(TestClassProperty)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(7, list.Count);

            // All
            Assert.Equal(1, list[0].Item1.Id);
            Assert.Equal("Master-1", list[0].Item1.Name);
            Assert.Equal(101, list[0].Item2.Id);
            Assert.Equal("Slave-101", list[0].Item2.Name);

            // All2
            Assert.Equal(1, list[1].Item1.Id);
            Assert.Equal("Master-1", list[1].Item1.Name);
            Assert.Equal(102, list[1].Item2.Id);
            Assert.Equal("Slave-102", list[1].Item2.Name);

            // Slave is NULL
            Assert.Equal(1, list[2].Item1.Id);
            Assert.Equal("Master-1", list[2].Item1.Name);
            Assert.Null(list[2].Item2);

            // Slave 1st is NULL
            Assert.Equal(1, list[3].Item1.Id);
            Assert.Equal("Master-1", list[3].Item1.Name);
            Assert.Null(list[3].Item2);

            // Slave 1st is not NULL
            Assert.Equal(1, list[4].Item1.Id);
            Assert.Equal("Master-1", list[4].Item1.Name);
            Assert.Equal(-1, list[4].Item2.Id);
            Assert.Null(list[4].Item2.Name);

            // Master is NULL
            Assert.Equal(0, list[5].Item1.Id);
            Assert.Null(list[5].Item1.Name);
            Assert.Equal(201, list[5].Item2.Id);
            Assert.Equal("Slave-201", list[5].Item2.Name);

            // All NULL
            Assert.Equal(0, list[6].Item1.Id);
            Assert.Null(list[6].Item1.Name);
            Assert.Null(list[6].Item2);
        }

        //--------------------------------------------------------------------------------
        // Class/Constructor
        //--------------------------------------------------------------------------------

        public class ClassConstructorMasterEntity
        {
            public int Id { get; }

            public string Name { get; }

            public ClassConstructorMasterEntity(int id, string name)
            {
                Id = id;
                Name = name;
            }
        }

        public class ClassConstructorSlaveEntity
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

            var info = new QueryInfo<Tuple<ClassConstructorMasterEntity, ClassConstructorSlaveEntity>>(engine, GetType().GetMethod(nameof(TestClassConstructor)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(7, list.Count);

            // All
            Assert.Equal(1, list[0].Item1.Id);
            Assert.Equal("Master-1", list[0].Item1.Name);
            Assert.Equal(101, list[0].Item2.Id);
            Assert.Equal("Slave-101", list[0].Item2.Name);

            // All2
            Assert.Equal(1, list[1].Item1.Id);
            Assert.Equal("Master-1", list[1].Item1.Name);
            Assert.Equal(102, list[1].Item2.Id);
            Assert.Equal("Slave-102", list[1].Item2.Name);

            // Slave is NULL
            Assert.Equal(1, list[2].Item1.Id);
            Assert.Equal("Master-1", list[2].Item1.Name);
            Assert.Null(list[2].Item2);

            // Slave 1st is NULL
            Assert.Equal(1, list[3].Item1.Id);
            Assert.Equal("Master-1", list[3].Item1.Name);
            Assert.Null(list[3].Item2);

            // Slave 1st is not NULL
            Assert.Equal(1, list[4].Item1.Id);
            Assert.Equal("Master-1", list[4].Item1.Name);
            Assert.Equal(-1, list[4].Item2.Id);
            Assert.Null(list[4].Item2.Name);

            // Master is NULL
            Assert.Equal(0, list[5].Item1.Id);
            Assert.Null(list[5].Item1.Name);
            Assert.Equal(201, list[5].Item2.Id);
            Assert.Equal("Slave-201", list[5].Item2.Name);

            // All NULL
            Assert.Equal(0, list[6].Item1.Id);
            Assert.Null(list[6].Item1.Name);
            Assert.Null(list[6].Item2);
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

            var info = new QueryInfo<Tuple<StructPropertyMasterEntity, StructPropertySlaveEntity>>(engine, GetType().GetMethod(nameof(TestStructProperty)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(7, list.Count);

            // All
            Assert.Equal(1, list[0].Item1.Id);
            Assert.Equal("Master-1", list[0].Item1.Name);
            Assert.Equal(101, list[0].Item2.Id);
            Assert.Equal("Slave-101", list[0].Item2.Name);

            // All2
            Assert.Equal(1, list[1].Item1.Id);
            Assert.Equal("Master-1", list[1].Item1.Name);
            Assert.Equal(102, list[1].Item2.Id);
            Assert.Equal("Slave-102", list[1].Item2.Name);

            // Slave is NULL
            Assert.Equal(1, list[2].Item1.Id);
            Assert.Equal("Master-1", list[2].Item1.Name);
            Assert.Equal(default, list[2].Item2);

            // Slave 1st is NULL
            Assert.Equal(1, list[3].Item1.Id);
            Assert.Equal("Master-1", list[3].Item1.Name);
            Assert.Equal(default, list[3].Item2);

            // Slave 1st is not NULL
            Assert.Equal(1, list[4].Item1.Id);
            Assert.Equal("Master-1", list[4].Item1.Name);
            Assert.Equal(-1, list[4].Item2.Id);
            Assert.Null(list[4].Item2.Name);

            // Master is NULL
            Assert.Equal(0, list[5].Item1.Id);
            Assert.Null(list[5].Item1.Name);
            Assert.Equal(201, list[5].Item2.Id);
            Assert.Equal("Slave-201", list[5].Item2.Name);

            // All NULL
            Assert.Equal(0, list[6].Item1.Id);
            Assert.Null(list[6].Item1.Name);
            Assert.Equal(default, list[6].Item2);
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

            var info = new QueryInfo<Tuple<StructConstructorMasterEntity, StructConstructorSlaveEntity>>(engine, GetType().GetMethod(nameof(TestStructConstructor)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(7, list.Count);

            // All
            Assert.Equal(1, list[0].Item1.Id);
            Assert.Equal("Master-1", list[0].Item1.Name);
            Assert.Equal(101, list[0].Item2.Id);
            Assert.Equal("Slave-101", list[0].Item2.Name);

            // All2
            Assert.Equal(1, list[1].Item1.Id);
            Assert.Equal("Master-1", list[1].Item1.Name);
            Assert.Equal(102, list[1].Item2.Id);
            Assert.Equal("Slave-102", list[1].Item2.Name);

            // Slave is NULL
            Assert.Equal(1, list[2].Item1.Id);
            Assert.Equal("Master-1", list[2].Item1.Name);
            Assert.Equal(default, list[2].Item2);

            // Slave 1st is NULL
            Assert.Equal(1, list[3].Item1.Id);
            Assert.Equal("Master-1", list[3].Item1.Name);
            Assert.Equal(default, list[3].Item2);

            // Slave 1st is not NULL
            Assert.Equal(1, list[4].Item1.Id);
            Assert.Equal("Master-1", list[4].Item1.Name);
            Assert.Equal(-1, list[4].Item2.Id);
            Assert.Null(list[4].Item2.Name);

            // Master is NULL
            Assert.Equal(0, list[5].Item1.Id);
            Assert.Null(list[5].Item1.Name);
            Assert.Equal(201, list[5].Item2.Id);
            Assert.Equal("Slave-201", list[5].Item2.Name);

            // All NULL
            Assert.Equal(0, list[6].Item1.Id);
            Assert.Null(list[6].Item1.Name);
            Assert.Equal(default, list[6].Item2);
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

            var info = new QueryInfo<Tuple<StructPropertyMasterEntity?, StructPropertySlaveEntity?>>(engine, GetType().GetMethod(nameof(TestNullableStructProperty)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(7, list.Count);

            // All
            Assert.NotNull(list[0].Item1);
            Assert.Equal(1, list[0].Item1.Value.Id);
            Assert.Equal("Master-1", list[0].Item1.Value.Name);
            Assert.NotNull(list[0].Item2);
            Assert.Equal(101, list[0].Item2.Value.Id);
            Assert.Equal("Slave-101", list[0].Item2.Value.Name);

            // All2
            Assert.NotNull(list[1].Item1);
            Assert.Equal(1, list[1].Item1.Value.Id);
            Assert.Equal("Master-1", list[1].Item1.Value.Name);
            Assert.NotNull(list[1].Item2);
            Assert.Equal(102, list[1].Item2.Value.Id);
            Assert.Equal("Slave-102", list[1].Item2.Value.Name);

            // Slave is NULL
            Assert.NotNull(list[2].Item1);
            Assert.Equal(1, list[2].Item1.Value.Id);
            Assert.Equal("Master-1", list[2].Item1.Value.Name);
            Assert.Null(list[2].Item2);

            // Slave 1st is NULL
            Assert.NotNull(list[3].Item1);
            Assert.Equal(1, list[3].Item1.Value.Id);
            Assert.Equal("Master-1", list[3].Item1.Value.Name);
            Assert.Null(list[3].Item2);

            // Slave 1st is not NULL
            Assert.NotNull(list[4].Item1);
            Assert.Equal(1, list[4].Item1.Value.Id);
            Assert.Equal("Master-1", list[4].Item1.Value.Name);
            Assert.NotNull(list[4].Item2);
            Assert.Equal(-1, list[4].Item2.Value.Id);
            Assert.Null(list[4].Item2.Value.Name);

            // Master is NULL
            Assert.NotNull(list[5].Item1);
            Assert.Equal(0, list[5].Item1.Value.Id);
            Assert.Null(list[5].Item1.Value.Name);
            Assert.NotNull(list[5].Item2);
            Assert.Equal(201, list[5].Item2.Value.Id);
            Assert.Equal("Slave-201", list[5].Item2.Value.Name);

            // All NULL
            Assert.NotNull(list[6].Item1);
            Assert.Equal(0, list[6].Item1.Value.Id);
            Assert.Null(list[6].Item1.Value.Name);
            Assert.Null(list[6].Item2);
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

            var info = new QueryInfo<Tuple<StructConstructorMasterEntity?, StructConstructorSlaveEntity?>>(engine, GetType().GetMethod(nameof(TestNullableStructConstructor)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(7, list.Count);

            // All
            Assert.NotNull(list[0].Item1);
            Assert.Equal(1, list[0].Item1.Value.Id);
            Assert.Equal("Master-1", list[0].Item1.Value.Name);
            Assert.NotNull(list[0].Item2);
            Assert.Equal(101, list[0].Item2.Value.Id);
            Assert.Equal("Slave-101", list[0].Item2.Value.Name);

            // All2
            Assert.NotNull(list[1].Item1);
            Assert.Equal(1, list[1].Item1.Value.Id);
            Assert.Equal("Master-1", list[1].Item1.Value.Name);
            Assert.NotNull(list[1].Item2);
            Assert.Equal(102, list[1].Item2.Value.Id);
            Assert.Equal("Slave-102", list[1].Item2.Value.Name);

            // Slave is NULL
            Assert.NotNull(list[2].Item1);
            Assert.Equal(1, list[2].Item1.Value.Id);
            Assert.Equal("Master-1", list[2].Item1.Value.Name);
            Assert.Null(list[2].Item2);

            // Slave 1st is NULL
            Assert.NotNull(list[3].Item1);
            Assert.Equal(1, list[3].Item1.Value.Id);
            Assert.Equal("Master-1", list[3].Item1.Value.Name);
            Assert.Null(list[3].Item2);

            // Slave 1st is not NULL
            Assert.NotNull(list[4].Item1);
            Assert.Equal(1, list[4].Item1.Value.Id);
            Assert.Equal("Master-1", list[4].Item1.Value.Name);
            Assert.NotNull(list[4].Item2);
            Assert.Equal(-1, list[4].Item2.Value.Id);
            Assert.Null(list[4].Item2.Value.Name);

            // Master is NULL
            Assert.NotNull(list[5].Item1);
            Assert.Equal(0, list[5].Item1.Value.Id);
            Assert.Null(list[5].Item1.Value.Name);
            Assert.NotNull(list[5].Item2);
            Assert.Equal(201, list[5].Item2.Value.Id);
            Assert.Equal("Slave-201", list[5].Item2.Value.Name);

            // All NULL
            Assert.NotNull(list[6].Item1);
            Assert.Equal(0, list[6].Item1.Value.Id);
            Assert.Null(list[6].Item1.Value.Name);
            Assert.Null(list[6].Item2);
        }

        //--------------------------------------------------------------------------------
        // NoMap/Master
        //--------------------------------------------------------------------------------

        public class NoMapMasterMasterEntity
        {
        }

        public class NoMapMasterSlaveEntity
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        [Fact]
        public void TestNoMapMaster()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(int), "Id"),
                new MockColumn(typeof(string), "Name"),
            };
            var values = new List<object[]>
            {
                new object[] { 101, "Slave-101" },
                new object[] { DBNull.Value, DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<Tuple<NoMapMasterMasterEntity, NoMapMasterSlaveEntity>>(engine, GetType().GetMethod(nameof(TestNoMapMaster)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);

            // All
            Assert.NotNull(list[0].Item1);
            Assert.NotNull(list[0].Item2);
            Assert.Equal(101, list[0].Item2.Id);
            Assert.Equal("Slave-101", list[0].Item2.Name);

            // All NULL
            Assert.NotNull(list[1].Item1);
            Assert.Null(list[1].Item2);
        }

        //--------------------------------------------------------------------------------
        // NoMap/Slave
        //--------------------------------------------------------------------------------

        public class NoMapSlaveMasterEntity
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class NoMapSlaveSlaveEntity
        {
        }

        [Fact]
        public void TestNoMapSlave()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(int), "Id"),
                new MockColumn(typeof(string), "Name"),
            };
            var values = new List<object[]>
            {
                new object[] { 1, "Master-1" },
                new object[] { DBNull.Value, DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<Tuple<NoMapSlaveMasterEntity, NoMapSlaveSlaveEntity>>(engine, GetType().GetMethod(nameof(TestNoMapSlave)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);

            // All
            Assert.NotNull(list[0].Item1);
            Assert.Equal(1, list[0].Item1.Id);
            Assert.Equal("Master-1", list[0].Item1.Name);
            Assert.Null(list[0].Item2);

            // All NULL
            Assert.NotNull(list[1].Item1);
            Assert.Equal(0, list[1].Item1.Id);
            Assert.Null(list[1].Item1.Name);
            Assert.Null(list[1].Item2);
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
                new MockColumn(typeof(string), "Name"),
            };
            var values = new List<object[]>
            {
                new object[] { 1, "Master-1" },
                new object[] { DBNull.Value, DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<Tuple<NoMapStructSlaveMasterEntity, NoMapStructSlaveSlaveEntity>>(engine, GetType().GetMethod(nameof(TestNoMapStructSlave)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);

            // All
            Assert.Equal(1, list[0].Item1.Id);
            Assert.Equal("Master-1", list[0].Item1.Name);
            Assert.Equal(default, list[0].Item2);

            // All NULL
            Assert.Equal(0, list[1].Item1.Id);
            Assert.Null(list[1].Item1.Name);
            Assert.Equal(default, list[1].Item2);
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
                new MockColumn(typeof(string), "Name"),
            };
            var values = new List<object[]>
            {
                new object[] { 1, "Master-1" },
                new object[] { DBNull.Value, DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<Tuple<NoMapStructSlaveMasterEntity?, NoMapStructSlaveSlaveEntity?>>(engine, GetType().GetMethod(nameof(TestNoMapStructSlave)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);

            // All
            Assert.NotNull(list[0].Item1);
            Assert.Equal(1, list[0].Item1.Value.Id);
            Assert.Equal("Master-1", list[0].Item1.Value.Name);
            Assert.Null(list[0].Item2);

            // All NULL
            Assert.NotNull(list[1].Item1);
            Assert.Equal(0, list[1].Item1.Value.Id);
            Assert.Null(list[1].Item1.Value.Name);
            Assert.Null(list[1].Item2);
        }
    }
}
