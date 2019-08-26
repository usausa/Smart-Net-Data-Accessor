# Smart.Data.Accessor .NET - data accessor generator library for .NET

## What is this?

* Build-time data accessor generator library.
* 2-way SQL supported.

## Getting Started(.NET Core Console Application)

Install library.

> PM> Install-Package [Usa.Smart.Data.Accessor](https://www.nuget.org/packages/Usa.Smart.Data.Accessor)

Create data accessor interafce and model class like this.

```csharp
using Smart.Data.Accessor.Attributes;

[Name("Data")]
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
        Console.WriteLine(typeA.Count);

        var all = dao.QueryDataList();
        Console.WriteLine(all.Count);
    }
}
```

## Attributes

### Data accessor attribute

(No documentation yet)

* DataAccessorAttribute

### Method attributes

(No documentation yet)

* QueryAttribute
* QueryFirstOrDefaultAttribute
* ExecuteAttribute
* ExecuteReaderAttribute
* ExecuteScalarAttribute

### Auto generate SQL method attributes

(No documentation yet)

* InsertAttribute
* ProcedureAttribute

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

(No documentation yet)

## Benchmark (for reference purpose only)

(Ref benchmark project)

## Example Project 

* [Console example](https://github.com/usausa/Smart-Net-Data-Accessor/tree/master/Example.ConsoleApplication)
* [Web example](https://github.com/usausa/Smart-Net-Data-Accessor/tree/master/Example.WebApplication)

## TODO

* Inject component to 2-way sql code. (ver 0.4+?)
* Enhanced auto generate SQL method attribute. (ver 0.5?)
* Enhanced Result mapper. (ver 0.6?)
* Enhanced handling of dynamic parameters. (ver 1.0+?)
