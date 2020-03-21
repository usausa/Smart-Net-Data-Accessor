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

            var mapper = new ResultMapperCache<int>(engine, false);

            var list = engine.QueryBuffer(cmd, mapper);

            Assert.Equal(2, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(0, list[1]);
        }
    }
}
