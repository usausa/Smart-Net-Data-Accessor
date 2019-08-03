namespace Smart.Data.Accessor.Engine
{
    using System.Collections.Generic;

    using Smart.Mock.Data;

    using Xunit;

    public class ResultMapperCacheTest
    {
        [Fact]
        public void ResultMapperCached()
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

            engine.QueryBuffer<CacheEntity>(cmd);

            Assert.Equal(1, ((IEngineController)engine).CountResultMapperCache);

            engine.QueryBuffer<CacheEntity>(cmd);

            Assert.Equal(1, ((IEngineController)engine).CountResultMapperCache);
        }

        [Fact]
        public void ResultMapperForSameTypeDifferentResult()
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

            engine.QueryBuffer<CacheEntity>(cmd);

            Assert.Equal(1, ((IEngineController)engine).CountResultMapperCache);

            engine.QueryBuffer<CacheEntity>(cmd);

            Assert.Equal(2, ((IEngineController)engine).CountResultMapperCache);

            engine.QueryBuffer<CacheEntity>(cmd);

            Assert.Equal(3, ((IEngineController)engine).CountResultMapperCache);
        }

        [Fact]
        public void ResultMapperForDifferentTypeSameResult()
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

            engine.QueryBuffer<CacheEntity>(cmd);

            Assert.Equal(1, ((IEngineController)engine).CountResultMapperCache);

            engine.QueryBuffer<Cache2Entity>(cmd);

            Assert.Equal(2, ((IEngineController)engine).CountResultMapperCache);

            ((IEngineController)engine).ClearResultMapperCache();

            Assert.Equal(0, ((IEngineController)engine).CountResultMapperCache);
        }

        public class CacheEntity
        {
            public long Id { get; set; }

            public string Name { get; set; }
        }

        public class Cache2Entity
        {
            public long Id { get; set; }

            public string Name { get; set; }
        }
    }
}
