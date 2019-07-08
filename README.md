# Smart.Data.Accessor .NET - dao generator library for .NET (PoC)

## What is this?

* Proof of concept implementation.
* Runtime dao generator by Roslyn.

### Usage example

(WIP)

```csharp
[Dao]
public interface IFullSpecDao
{
    [Execute] int Execute();
    [Execute] Task<int> ExecuteAsync(CancellationToken cancel);
    [Execute] int Execute(DbConnection con);
    [Execute] Task<int> ExecuteAsync(DbConnection con, CancellationToken cancel);

    [ExecuteScalar] long ExecuteScalar();
    [ExecuteScalar] Task<long> ExecuteScalarAsync(CancellationToken cancel);
    [ExecuteScalar] long ExecuteScalar(DbConnection con);
    [ExecuteScalar] Task<long> ExecuteScalarAsync(DbConnection con, CancellationToken cancel);

    [ExecuteScalarReader] IDataReader ExecuteReader();
    [ExecuteScalarReader] Task<IDataReader> ExecuteReaderAsync(CancellationToken cancel);
    [ExecuteScalarReader] IDataReader ExecuteReader(DbConnection con);
    [ExecuteScalarReader] Task<IDataReader> ExecuteReaderAsync(DbConnection con, CancellationToken cancel);

    [Query] IEnumerable<DataEntity> QueryNonBuffer();
    [Query] Task<IEnumerable<DataEntity>> QueryNonBufferAsync(CancellationToken cancel);
    [Query] IEnumerable<DataEntity> QueryNonBuffer(DbConnection con);
    [Query] Task<IEnumerable<DataEntity>> QueryNonBufferAsync(DbConnection con, CancellationToken cancel);

    [Query] IList<DataEntity> QueryBuffer();
    [Query] Task<IList<DataEntity>> QueryBufferAsync(CancellationToken cancel);
    [Query] IList<DataEntity> QueryBuffer(DbConnection con);
    [Query] Task<IList<DataEntity>> QueryBufferAsync(DbConnection con, CancellationToken cancel);

    [QueryFirstOrDefault] DataEntity QueryFirstOrDefault();
    [QueryFirstOrDefault] Task<DataEntity> QueryFirstOrDefaultAsync(CancellationToken cancel);
    [QueryFirstOrDefault] DataEntity QueryFirstOrDefault(DbConnection con);
    [QueryFirstOrDefault] Task<DataEntity> QueryFirstOrDefaultAsync(DbConnection con, CancellationToken cancel);

    [Execute] int ExecuteEnumerable([AnsiString(3)] string[] ids);

    [Procedure("TEST1")] int CallTest1(ProcParameter parameter, [TimeoutParameter] int timeout);
    [Procedure("TEST2")] void CallTest2(int param1, ref int param2, out int param3);

    [Insert] int Insert(DataEntity entity);
}
```

### Functions

(WIP)

### TODO

* Add DI integration
* Add sample
* Add unit test
* Add benchmark
* Add other builder attributes like update, delete
* Component injection support to sql code
* LIKE escape support(?)
* Dynamic parameter support
