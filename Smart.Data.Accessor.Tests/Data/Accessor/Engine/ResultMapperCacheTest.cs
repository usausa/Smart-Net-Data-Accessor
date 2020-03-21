namespace Smart.Data.Accessor.Engine
{
    using System.Collections.Generic;

    using Smart.Mock.Data;

    using Xunit;

    public class ResultMapperCacheTest
    {
        [Fact]
        public void TestResultMapperCache()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name")
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, new List<object[]>()));
            cmd.SetupResult(new MockDataReader(columns, new List<object[]>()));

            var mapper = new ResultMapperCache<CacheEntity>(engine, false);

            engine.QueryBuffer(cmd, mapper);

            Assert.Equal(1, mapper.Depth);

            engine.QueryBuffer(cmd, mapper);

            Assert.Equal(1, mapper.Depth);
        }

        [Fact]
        public void TestResultMapperCacheOptimized()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns = new[]
            {
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name")
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns, new List<object[]>()));
            cmd.SetupResult(new MockDataReader(columns, new List<object[]>()));

            var mapper = new ResultMapperCache<CacheEntity>(engine, true);

            engine.QueryBuffer(cmd, mapper);

            Assert.Equal(1, mapper.Depth);

            engine.QueryBuffer(cmd, mapper);

            Assert.Equal(1, mapper.Depth);
        }

        [Fact]
        public void TestResultMapperCacheForSameTypeDifferentResult()
        {
            var engine = new ExecuteEngineConfig().ToEngine();

            var columns1 = new[]
            {
                new MockColumn(typeof(long), "Id")
            };
            var columns2 = new[]
            {
                new MockColumn(typeof(string), "Name")
            };
            var columns3 = new[]
            {
                new MockColumn(typeof(long), "Id"),
                new MockColumn(typeof(string), "Name")
            };

            var cmd = new MockDbCommand();
            cmd.SetupResult(new MockDataReader(columns1, new List<object[]>()));
            cmd.SetupResult(new MockDataReader(columns2, new List<object[]>()));
            cmd.SetupResult(new MockDataReader(columns3, new List<object[]>()));

            var mapper = new ResultMapperCache<CacheEntity>(engine, false);

            engine.QueryBuffer(cmd, mapper);

            Assert.Equal(1, mapper.Depth);

            engine.QueryBuffer(cmd, mapper);

            Assert.Equal(2, mapper.Depth);

            engine.QueryBuffer(cmd, mapper);

            Assert.Equal(3, mapper.Depth);
        }

        public class CacheEntity
        {
            public long Id { get; set; }

            public string Name { get; set; }
        }
    }
}
