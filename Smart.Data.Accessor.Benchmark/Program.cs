namespace Smart.Data.Accessor.Benchmark
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    using BenchmarkDotNet.Attributes;
    using BenchmarkDotNet.Configs;
    using BenchmarkDotNet.Diagnosers;
    using BenchmarkDotNet.Exporters;
    using BenchmarkDotNet.Jobs;
    using BenchmarkDotNet.Running;

    using Dapper;

    using Smart.Data.Accessor.Attributes;
    using Smart.Data.Accessor.Engine;
    using Smart.Data.Accessor.Generator;
    using Smart.Data.Accessor.Loaders;
    using Smart.Mock.Data;

    public static class Program
    {
        public static void Main()
        {
            //var b = new DaoBenchmark();
            //b.IterationSetup();
            //b.DapperExecute();
            //b.IterationSetup();
            //b.SmartExecute();
            BenchmarkRunner.Run<DaoBenchmark>();
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.Default, MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            //Add(Job.LongRun);
            Add(Job.MediumRun);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Ignore")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Ignore")]
    [Config(typeof(BenchmarkConfig))]
    public class DaoBenchmark
    {
        private MockRepeatDbConnection mockExecute;
        private MockRepeatDbConnection mockExecuteScalar;
        private MockRepeatDbConnection mockQuery;
        private MockRepeatDbConnection mockQueryFirst;

        private IBenchmarkDao dapperExecuteDao;
        private IBenchmarkDao smartExecuteDao;

        // TODO

        [GlobalSetup]
        public void Setup()
        {
            mockExecute = new MockRepeatDbConnection(1);

            mockExecuteScalar = new MockRepeatDbConnection(1L);

            mockQuery = new MockRepeatDbConnection(new MockDataReader(
                new[]
                {
                    new MockColumn(typeof(long), "Id"),
                    new MockColumn(typeof(string), "Name")
                },
                Enumerable.Range(1, 100).Select(x => new object[]
                {
                    (long)x,
                    "test"
                })));

            mockQueryFirst = new MockRepeatDbConnection(new MockDataReader(
                new[]
                {
                    new MockColumn(typeof(long), "Id"),
                    new MockColumn(typeof(string), "Name"),
                    new MockColumn(typeof(int), "Amount"),
                    new MockColumn(typeof(int), "Qty"),
                    new MockColumn(typeof(bool), "Flag1"),
                    new MockColumn(typeof(bool), "Flag2"),
                    new MockColumn(typeof(DateTimeOffset), "CreatedAt"),
                    new MockColumn(typeof(string), "CreatedBy"),
                    new MockColumn(typeof(DateTimeOffset?), "UpdatedAt"),
                    new MockColumn(typeof(string), "UpdatedBy"),
                },
                Enumerable.Range(1, 1).Select(x => new object[]
                {
                    (long)x,
                    "test",
                    1,
                    2,
                    true,
                    false,
                    DateTimeOffset.Now,
                    "user",
                    DBNull.Value,
                    DBNull.Value
                })));

            // DAO
            var executeProvider = new DelegateDbProvider(() => mockExecute);
            dapperExecuteDao = new DapperDao(executeProvider);
            smartExecuteDao = CreateSmartDao(executeProvider);
        }

        private static IBenchmarkDao CreateSmartDao(IDbProvider provider)
        {
            var engine = new ExecuteEngineConfig()
                .ConfigureComponents(components =>
                {
                    components.Add(provider);
                })
                .ToEngine();

            var generator = new DaoGenerator(engine, new DummyLoader());
            return generator.Create<IBenchmarkDao>();
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            mockExecute.Dispose();
            mockExecuteScalar.Dispose();
            mockQuery.Dispose();
            mockQueryFirst.Dispose();
        }

        //--------------------------------------------------------------------------------
        // Execute
        //--------------------------------------------------------------------------------

        [Benchmark]
        public void DapperExecute() => dapperExecuteDao.Execute(new DataEntity { Id = 1, Name = "xxx" });

        [Benchmark]
        public void SmartExecute() => smartExecuteDao.Execute(new DataEntity { Id = 1, Name = "xxx" });

        // TODO
    }

    [Dao]
    public interface IBenchmarkDao
    {
        [Execute]
        int Execute(DataEntity entity);

        // TODO
    }

    public class DummyLoader : ISqlLoader
    {
        private readonly Dictionary<string, string> sql = new Dictionary<string, string>
        {
            {
                // TODO entity対応
                "Execute",
                "INSERT INTO Table (Id, Name) VALUES (/*@ entity.Id */1, /*@ entity.Name */'xxx')"
            }
        };

#pragma warning disable CA1062 // Validate arguments of public methods
        public string Load(MethodInfo mi) => sql[mi.Name];
#pragma warning restore CA1062 // Validate arguments of public methods
    }

    public sealed class DapperDao : IBenchmarkDao
    {
        private readonly IDbProvider provider;

        public DapperDao(IDbProvider provider)
        {
            this.provider = provider;
        }

        public int Execute(DataEntity entity)
        {
            using (var con = provider.CreateConnection())
            {
                return con.Execute("INSERT INTO Table (Id, Name) VALUES (@Id, @Name)", entity);
            }
        }

        // TODO
    }

    public class DataEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }
    }

    public class LargeDataEntity
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public int Amount { get; set; }

        public int Qty { get; set; }

        public bool Flag1 { get; set; }

        public bool Flag2 { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public string CreatedBy { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string UpdatedBy { get; set; }
    }
}
