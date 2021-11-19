namespace Smart.Data.Accessor.Benchmark;

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using Dapper;

using Smart.Data.Accessor.Attributes;
using Smart.Data.Accessor.Engine;
using Smart.Mock.Data;

public static class Program
{
    public static void Main()
    {
        BenchmarkRunner.Run<AccessorBenchmark>();
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.Default, MarkdownExporter.GitHub);
        AddDiagnoser(MemoryDiagnoser.Default);
        //AddJob(Job.LongRun);
        AddJob(Job.MediumRun);
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Ignore")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Justification = "Ignore")]
[Config(typeof(BenchmarkConfig))]
public class AccessorBenchmark
{
    [AllowNull]
    private MockRepeatDbConnection mockExecute;
    [AllowNull]
    private MockRepeatDbConnection mockExecuteScalar;
    [AllowNull]
    private MockRepeatDbConnection mockQuery;
    [AllowNull]
    private MockRepeatDbConnection mockQueryFirst;

    [AllowNull]
    private IBenchmarkAccessorForDapper dapperExecuteAccessor;
    [AllowNull]
    private IBenchmarkAccessor smartExecuteAccessor;

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
                new MockColumn(typeof(string), "Name")
            },
            Enumerable.Range(1, 1).Select(x => new object[]
            {
                (long)x,
                "test"
            })));

        // DAO
        dapperExecuteAccessor = new DapperAccessor();

        var engine = new ExecuteEngineConfig()
            .ToEngine();
        var factory = new DataAccessorFactory(engine);
        smartExecuteAccessor = factory.Create<IBenchmarkAccessor>();
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
    public int DapperExecute() => dapperExecuteAccessor.Execute(mockExecute, new DataEntity { Id = 1, Name = "xxx" });

    [Benchmark]
    public int SmartExecute() => smartExecuteAccessor.Execute(mockExecute, new DataEntity { Id = 1, Name = "xxx" });

    [Benchmark]
    public long DapperExecuteScalar() => dapperExecuteAccessor.ExecuteScalar(mockExecuteScalar);

    [Benchmark]
    public long SmartExecuteScalar() => smartExecuteAccessor.ExecuteScalar(mockExecuteScalar);

    [Benchmark]
    public long DapperQueryBufferd100() => dapperExecuteAccessor.QueryBufferd(mockQuery).Count();

    [Benchmark]
    public long SmartQueryBufferd100() => smartExecuteAccessor.QueryBufferd(mockQuery).Count;

    [Benchmark]
    public long SmartQueryBufferd100Optimized() => smartExecuteAccessor.QueryBufferdOptimized(mockQuery).Count;

    [Benchmark]
    public DataEntity DapperQueryFirstOrDefault() => dapperExecuteAccessor.QueryFirstOrDefault(mockQueryFirst, 1);

    [Benchmark]
    public DataEntity SmartQueryFirstOrDefault() => smartExecuteAccessor.QueryFirstOrDefault(mockQueryFirst, 1);

    [Benchmark]
    public DataEntity SmartQueryFirstOrDefaultOptimized() => smartExecuteAccessor.QueryFirstOrDefaultOptimized(mockQueryFirst, 1);

    [Benchmark]
    public long DapperWithCondition() => dapperExecuteAccessor.ExecuteScalarWithCondition(mockExecuteScalar, "1");

    [Benchmark]
    public long SmartWithCondition() => smartExecuteAccessor.ExecuteScalarWithCondition(mockExecuteScalar, "1");
}

[DataAccessor]
public interface IBenchmarkAccessor
{
    [Execute]
    int Execute(DbConnection con, DataEntity entity);

    [ExecuteScalar]
    long ExecuteScalar(DbConnection con);

    [ExecuteScalar]
    long ExecuteScalarWithCondition(DbConnection con, string? flag);

    [Query]
    List<DataEntity> QueryBufferd(DbConnection con);

    [QueryFirstOrDefault]
    DataEntity QueryFirstOrDefault(DbConnection con, long id);

    [Query]
    [MethodName(nameof(QueryBufferd))]
    [Optimize(true)]
    List<DataEntity> QueryBufferdOptimized(DbConnection con);

    [QueryFirstOrDefault]
    [MethodName(nameof(QueryFirstOrDefault))]
    [Optimize(true)]
    DataEntity QueryFirstOrDefaultOptimized(DbConnection con, long id);
}

public interface IBenchmarkAccessorForDapper
{
    int Execute(DbConnection con, DataEntity entity);

    long ExecuteScalar(DbConnection con);

    long ExecuteScalarWithCondition(DbConnection con, string? flag);

    IEnumerable<DataEntity> QueryBufferd(DbConnection con);

    DataEntity QueryFirstOrDefault(DbConnection con, long id);
}

public sealed class DapperAccessor : IBenchmarkAccessorForDapper
{
    public int Execute(DbConnection con, DataEntity entity)
    {
        return con.Execute("INSERT INTO Data (Id, Name) VALUES (@Id, @Name)", entity);
    }

    public long ExecuteScalar(DbConnection con)
    {
        return con.ExecuteScalar<long>("SELECT COUNT(*) FROM Data");
    }

    public long ExecuteScalarWithCondition(DbConnection con, string? flag)
    {
        var sql = new StringBuilder();
        sql.Append("SELECT COUNT(*) FROM Data");
        if (flag is not null)
        {
            sql.Append(" WHERE Flag = @flag");
        }

        return con.ExecuteScalar<long>(sql.ToString(), new { flag });
    }

    public IEnumerable<DataEntity> QueryBufferd(DbConnection con)
    {
        return con.Query<DataEntity>("SELECT * FROM Data");
    }

    public DataEntity QueryFirstOrDefault(DbConnection con, long id)
    {
        return con.QueryFirstOrDefault<DataEntity>("SELECT * FROM Data WHERE Id = @Id", new { Id = id });
    }
}

public class DataEntity
{
    public long Id { get; set; }

    [AllowNull]
    public string Name { get; set; }
}
