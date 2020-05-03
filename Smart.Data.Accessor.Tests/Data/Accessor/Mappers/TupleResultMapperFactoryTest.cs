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
        // Map
        //--------------------------------------------------------------------------------

        public class MasterEntity
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        public class SlaveEntity
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }

        [Fact]
        public void TestMapTuple()
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
                new object[] { DBNull.Value, DBNull.Value, 201, "Slave-201" },
                new object[] { DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<Tuple<MasterEntity, SlaveEntity>>(engine, GetType().GetMethod(nameof(TestMapTuple)), false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(5, list.Count);

            Assert.Equal(1, list[0].Item1.Id);
            Assert.Equal("Master-1", list[0].Item1.Name);
            Assert.Equal(101, list[0].Item2.Id);
            Assert.Equal("Slave-101", list[0].Item2.Name);

            Assert.Equal(1, list[1].Item1.Id);
            Assert.Equal("Master-1", list[1].Item1.Name);
            Assert.Equal(102, list[1].Item2.Id);
            Assert.Equal("Slave-102", list[1].Item2.Name);

            Assert.Equal(1, list[2].Item1.Id);
            Assert.Equal("Master-1", list[2].Item1.Name);
            Assert.Equal(0, list[2].Item2.Id);
            Assert.Null(list[2].Item2.Name);

            Assert.Equal(0, list[3].Item1.Id);
            Assert.Null(list[3].Item1.Name);
            Assert.Equal(201, list[3].Item2.Id);
            Assert.Equal("Slave-201", list[3].Item2.Name);

            Assert.Equal(0, list[4].Item1.Id);
            Assert.Null(list[4].Item1.Name);
            Assert.Equal(0, list[4].Item2.Id);
            Assert.Null(list[4].Item2.Name);
        }
    }
}
