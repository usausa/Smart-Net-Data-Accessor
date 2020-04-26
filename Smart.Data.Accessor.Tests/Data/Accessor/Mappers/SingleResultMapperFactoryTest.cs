namespace Smart.Data.Accessor.Mappers
{
    using System;
    using System.Collections.Generic;

    using Smart.Data.Accessor.Engine;
    using Smart.Mock.Data;

    using Xunit;

    public class SingleResultMapperFactoryTest
    {
        //--------------------------------------------------------------------------------
        // Query
        //--------------------------------------------------------------------------------

        [Fact]
        public void TestMapSingle()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(int), "Column1")
            };
            var values = new List<object[]>
            {
                new object[] { 1 },
                new object[] { DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<int>(engine, null, false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(0, list[1]);
        }

        [Fact]
        public void TestMapSingleWithConvert()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(int), "Column1")
            };
            var values = new List<object[]>
            {
                new object[] { 1 },
                new object[] { DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<string>(engine, null, false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);
            Assert.Equal("1", list[0]);
            Assert.Null(list[1]);
        }

        [Fact]
        public void TestMapSingleNullable()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(int), "Column1")
            };
            var values = new List<object[]>
            {
                new object[] { 1 },
                new object[] { DBNull.Value }
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, values));

            var info = new QueryInfo<int?>(engine, null, false);

            var list = engine.QueryBuffer(info, cmd);

            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Null(list[1]);
        }
    }
}
