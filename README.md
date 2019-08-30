# Smart.Data.Accessor .NET - data accessor generator library for .NET

## What is this?

* Build-time data accessor generator library.
* 2-way SQL supported.

## Getting Started(.NET Core Console Application)

Install [Usa.Smart.Data.Accessor](https://www.nuget.org/packages/Usa.Smart.Data.Accessor).

Create data accessor interafce and model class like this.

```csharp
using Smart.Data.Accessor.Attributes;

public class DataEntity
{
    public long Id { get; set; }

    public string Name { get; set; }

    public string Type { get; set; }
}
```

```csharp
using System.Collections.Generic;

using Smart.Data.Accessor.Attributes;

[DataAccessor]
public interface IExampleDao
{
    [Execute]
    void Create();

    [Insert]
    void Insert(DataEntity entity);

    [Query]
    List<DataEntity> QueryDataList(string type = null);
}
```

Create an SQL file with the naming convention of interface name + method name.

Methods with [Insert] attribute automatically generate SQL, so no file is required.

By default, SQL files are placed in the Sql subfolder of the interface file.

* IExampleDao.Create.sql

```sql
CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text, Type text)
```

* IExampleDao.QueryDataList.sql

```sql
SELECT * FROM Data
/*% if (!String.IsNullOrEmpty(type)) { */
WHERE Type = /*@ type */'A'
/*% } */
```

Use as follows.

```csharp
using System;
using System.IO;

using Microsoft.Data.Sqlite;

using Smart.Data;
using Smart.Data.Accessor;
using Smart.Data.Accessor.Engine;

public static class Program
{
    public static void Main()
    {
        // Initialize
        var engine = new ExecuteEngineConfig()
            .ConfigureComponents(c =>
            {
                c.Add<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection("Data Source=test.db")));
            })
            .ToEngine();
        var factory = new DataAccessorFactory(engine);

        // Create data accessor
        var dao = factory.Create<IExampleDao>();

        // Create
        dao.Create();

        // Insert
        dao.Insert(new DataEntity { Id = 1L, Name = "Data-1", Type = "A" });
        dao.Insert(new DataEntity { Id = 2L, Name = "Data-2", Type = "B" });
        dao.Insert(new DataEntity { Id = 3L, Name = "Data-3", Type = "A" });

        // Query
        var typeA = dao.QueryDataList("A");
        Console.WriteLine(typeA.Count); // 2

        var all = dao.QueryDataList();
        Console.WriteLine(all.Count); // 3
    }
}
```

## Attributes

### Data accessor attribute

* DataAccessorAttribute

Data accessor interface marker.

### Method attributes

* ExecuteAttribute

```csharp
[DataAccessor]
public interface IExecuteDao
{
    // Call ExecuteNonQuery()
    [Execute]
    int Update(long id, string name);

    [Execute]
    ValueTask<int> UpdateAsync(long id, string name);
}
```

* ExecuteScalarAttribute

```csharp
[DataAccessor]
public interface IExecuteScalarDao
{
    // Call ExecuteScalar()
    [ExecuteScalar]
    long Count();

    [ExecuteScalar]
    ValueTask<long> CountAsync();
}
```
* ExecuteReaderAttribute

```csharp
[DataAccessor]
public interface IExecuteReaderDao
{
    // Call ExecuteReader()
    [ExecuteReader]
    IDataReader Enumerate();

    [ExecuteReader]
    ValueTask<IDataReader> EnumerateAsync();
}
```

* QueryFirstOrDefaultAttribute

```csharp
[DataAccessor]
public interface IQueryFirstOrDefaultDao
{
    // Call ExecuteReader() and map single object or default
    [QueryFirstOrDefault]
    DataEntity QueryData(long id);

    [QueryFirstOrDefault]
    ValueTask<DataEntity> QueryDataAsync(long id);
}
```

* QueryAttribute

```csharp
[DataAccessor]
public interface IQueryDao
{
    // Call ExecuteReader() and map object list bufferd
    [Query]
    IList<DataEntity> QueryBufferd();

    // Call ExecuteReader() and map object enumerable non-bufferd
    [Query]
    IEnumerable<DataEntity> QueryNonBufferd();

    [Query]
    ValueTask<IList<DataEntity>> QueryBufferdAsync();

    [Query]
    ValueTask<IEnumerable<DataEntity>> QueryNonBufferdAsync();
}
```

### Auto generate SQL method attributes

(No documentation yet)

* InsertAttribute

* ProcedureAttribute

```sql
CREATE PROCEDURE PROC1
    @param1 INT,
    @param2 INT OUTPUT,
    @param3 INT OUTPUT
AS
BEGIN
    SELECT @param2 = @param2 + 1
    SELECT @param3 = @param1 + 1
    RETURN 100
END
```

```csharp
public class Parameter
{
    [Input]
    [Name("param1")]
    public int Parameter1 { get; set; }

    [InputOutput]
    [Name("param2")]
    public int Parameter2 { get; set; }

    [Output]
    [Name("param3")]
    public int Parameter3 { get; set; }

    [ReturnValue]
    public int ReturnValue { get; set; }
}
```

```csharp
[DataAccessor]
public interface IProcedureDao
{
    // Argument version
    [Procedure("PROC1")]
    int Execute(int param1, ref int param2, out int param3);

    // Parameter class version
    [Procedure("PROC1")]
    void Execute(Parameter parameter);
}
```

```csharp
var param2 = 2;
var ret = dao.Execute(1, ref param2, out var param3);
// param2 = 3, param3 = 2, ret = 100
```

```csharp
var parameter = new Parameter { Parameter1 = 1, Parameter2 = 2 };
dao.Execute(parameter);
// Parameter2 = 3, Parameter3 = 2, ReturnValue = 100
```

### Mapping attributes

(No documentation yet)

* IgnoreAttribute
* NameAttribute
* DirectionAttribute

### Parameter builder attributes

(No documentation yet)

* AnsiStringAttribute
* DbTypeAttribute

### Connection selector attribute

(No documentation yet)

* ProviderAttribute

### Result parser attribute

(No documentation yet)

* ResultParserAttribute

### Option attribute

(No documentation yet)

* TimeoutAttribute
* TimeoutParameterAttribute
* SqlSizeAttribute

## Special arguments

### DbConnection

(No documentation yet)

### DbTransaction

(No documentation yet)

### CancellationToken

(No documentation yet)

## 2-way SQL

|   | Type          | Example                                     |
|:-:|---------------|---------------------------------------------|
| @ | parameter     | `/*@ id */`                                 |
| % | code block    | `/*% if (!String.IsNullOrEmpty(name)) { */` |
| # | raw parameter | `/*# order #/`                              |
| ! | pragma        | `/*!using System.Text */`                   |

### Parameter

(No documentation yet)

### Code block

(No documentation yet)

### Raw parameter

(No documentation yet)

### Pragma

(No documentation yet)

## Configuration

### IDbProvider

(No documentation yet)

### Type map

(No documentation yet)

### Type handler

(No documentation yet)

### Result mapper factory

(No documentation yet)

## ASP.NET Core integration

```csharp
services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection("Data Source=test.db")));

services.AddDataAccessor(config =>
{
    config.DaoAssemblies.Add(Assembly.GetExecutingAssembly());
});
```

```csharp
private readonly ISampleDao sampleDao;

public HomeController(ISampleDao sampleDao)
{
    this.sampleDao = sampleDao;
}
```

## Code generation

(No documentation yet)

## Benchmark (for reference purpose only)

(Ref benchmark project)

## Example Project

* [Console example](https://github.com/usausa/Smart-Net-Data-Accessor/tree/master/Example.ConsoleApplication)
* [Web example](https://github.com/usausa/Smart-Net-Data-Accessor/tree/master/Example.WebApplication)

## TODO

* Enhanced auto generate SQL method attribute. (ver 0.6?)
* Enhanced Result mapper. (ver 0.7?)
* Enhanced handling of dynamic parameters. (ver 1.0+?)
