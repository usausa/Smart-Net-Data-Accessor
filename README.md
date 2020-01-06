# Smart.Data.Accessor .NET - data accessor generator library for .NET

## What is this?

* Build-time data accessor generator library.
* 2-way SQL supported.

## Getting Started(.NET Core Console Application)

Install [Usa.Smart.Data.Accessor](https://www.nuget.org/packages/Usa.Smart.Data.Accessor).

Create data accessor interface and model class like this.

```csharp
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
public interface IExampleAccessor
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

By default, SQL files are placed in the 'Sql' subfolder of the interface file.

* IExampleAccessor.Create.sql

```sql
CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text, Type text)
```

* IExampleAccessor.QueryDataList.sql

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
        var dao = factory.Create<IExampleAccessor>();

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

## 2-way SQL

|   | Type          | Example                                     |
|:-:|---------------|---------------------------------------------|
| @ | parameter     | `/*@ id */`                                 |
| # | raw parameter | `/*# order #/`                              |
| % | code block    | `/*% if (!String.IsNullOrEmpty(name)) { */` |
| ! | pragma        | `/*!using System.Text */`                   |

### Parameter

```sql
SELECT * FROM Data WHERE Id = /*@ id */1
```

### Raw parameter

```sql
SELECT * FROM Data ORDER BY /*# order */Name
```

### Code block

```sql
SELECT * FROM Data
/*% if (IsNotNull(id)) { */
WHERE Id >= /*@ id */0
/*% } */
```

### Pragma

* Using

```sql
/*!using System.Text */
```

* Using static

```csharp
public static class CustomScriptHelper
{
    public static bool HasValue(int? value)
    {
        return value.HasValue;
    }
}
```

```sql
/*!helper MyLibrary.CustomScriptHelper */
SELECT * FROM Data
/*% if (HasValue(id)) { */
WHERE Id >= /*@ id */0
*% } *
```

### Built-in helper

```csharp
public static class ScriptHelper
{
    public static bool IsNull(object value);

    public static bool IsNotNull(object value);

    public static bool IsEmpty(string value);

    public static bool IsNotEmpty(string value);

    public static bool Any(Array array);

    public static bool Any(ICollection ic);
}
```

## Attributes

### Data accessor attribute

* DataAccessorAttribute

```csharp
// Data accessor interface marker
[DataAccessor]
public interface IExampleAccessor
{
...
}
```

### Method attributes

* ExecuteAttribute

```csharp
[DataAccessor]
public interface IExecuteAccessor
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
public interface IExecuteScalarAccessor
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
public interface IExecuteReaderAccessor
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
public interface IQueryFirstOrDefaultAccessor
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
public interface IQueryAccessor
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
    IAsyncEnumerable<DataEntity> QueryNonBufferdAsync();
}
```

### Mapping attributes

* IgnoreAttribute

```csharp
public class DataEntity
{
    // Ignore mapping
    [Ignore]
    public int IgnoreMember { get; set; }
}
```

* NameAttribute

```csharp
public class UserEntity
{
    // Map from USER_NAME column
    [Name("USER_NAME")]
    public string UserName { get; set; }
}
```

* DirectionAttribute

```csharp
public class Parameter
{
    // ParameterDirection.Input is used
    [Input]
    public int InputParameter { get; set; }

    // ParameterDirection.InputOutput is used
    [InputOutput]
    public int InputOutputParameter { get; set; }

    // ParameterDirection.Output is used
    [Output]
    public int OutputParameter { get; set; }

    // ParameterDirection.ReturnValue is used
    [ReturnValue]
    public int ReturnValue { get; set; }
}
```

### Parameter builder attributes

* AnsiStringAttribute

```csharp
[DataAccessor]
public interface IAnsiStringAccessor
{
    // DbType.AnsiStringFixedLength is set
    [QueryFirstOrDefault]
    DataEntity QueryEntity([AnsiString(3)] string id);
}
```

* DbTypeAttribute

```csharp
public class Parameter
{
    // DbType.AnsiStringFixedLength is set
    [DbType(DbType.AnsiStringFixedLength, 3)]
    public string Id { get; set; }
}
```

```csharp
[DataAccessor]
public interface IDbTypeAccessor
{
    [QueryFirstOrDefault]
    DataEntity QueryEntity(Parameter parameter);
}
```

### Result parser attribute

* ResultParserAttribute

```csharp
public sealed class CustomParserAttribute : ResultParserAttribute
{
    public override Func<object, object> CreateParser(IServiceProvider serviceProvider, Type type)
    {
        return x => Convert.ChangeType(x, type, CultureInfo.InvariantCulture);
    }
}
```

```csharp
public class ParserEntity
{
    // DB value parsed by CustomParserAttribute
    [CustomParser]
    public long Value { get; set; }
}
```

### Injection attribute

```csharp
public class Counter
{
    private long counter;

    public long Next() => ++counter;
}
```

```csharp
[DataAccessor]
[Inject(typeof(Counter), "counter")]
public interface IInjectAccessor
{
...
}
```

```sql
INSERT INTO Data (Value) VALUES (/*@ counter.Next() */)
```

### Connection selector attribute

* ProviderAttribute

```csharp
// IDbProvider named 'Primary' selected by IDbProviderSelector
[DataAccessor]
[Provider("Primary")]
public interface IPrimaryAccessor
{
...
}
```

```csharp
// IDbProvider named 'Secondary' selected by IDbProviderSelector
[DataAccessor]
[Provider("Secondary")]
public interface ISecondaryAccessor
{
...
}
```

### Option attribute

* TimeoutAttribute

```csharp
[DataAccessor]
public interface ITimeoutAccessor
{
    // timeout is used for IDbCommand.CommandTimeout
    [Execute]
    int Execute([Timeout] int timeout);
}
```

* CommandTimeoutAttribute

```csharp
[DataAccessor]
public interface ICommandTimeoutAccessor
{
    // IDbCommand.CommandTimeout = 300000;
    [Execute]
    [CommandTimeout(30000)]
    int Execute();
}
```

## SQL builder method attributes

Attributes that automatically generate SQL.

It is extensible and can implement its own attributes.

### Builder attribute

* InsertAttribute

```csharp
[DataAccessor]
public interface IInsertAccessor
{
    // DataEntity property is used
    [Insert]
    int Insert(DataEntity entity);

    // Method arguments is used
    [Insert(typeof(DataEntity))]
    int Insert(long id, string name);
}
```

* UpdateAttribute

```csharp
public class UpdateValues
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; }
}
```

```csharp
public class UpdateValues
{
    public string Type { get; set; }

    public string Name { get; set; }
}
```

```csharp
[DataAccessor]
public interface IUpdateAccessor
{
    // By entity key memember
    [Update]
    int Update(DataEntity entity);

    // UPDATE Type and Name by id
    [Update(typeof(DataEntity))]
    int Update([Values] UpdateValues values, long id);
}
```

* DeleteAttribute

```csharp
[DataAccessor]
public interface IDeleteAccessor
{
    // Id = /*@ id */
    [Delete]
    int Delete(long id);

    // By entity key memember
    [Delete]
    int Delete(DataEntity entity);

    // Force option is required to delete all
    [Delete(typeof(DataEntity), Force = true)]
    int DeleteAll();

    // Key1 = @key1 AND Key2 >= @key2
    [Delete]
    int Delete(long key1, [Condition(Operand.GreaterEqualThan)] long key2);
}
```

* SelectAttribute

```csharp
[DataAccessor]
public interface ISelectAccessor
{
    // Conditoon

    // Key1 = @key1 AND Key2 >= @key2
    [Select]
    List<DataEntity> SelectListByCondition(long key1, [Condition(Operand.GreaterEqualThan)] long key2);

    // Order

    // Key order is default
    [Select]
    List<DataEntity> SelectListKeyOrder();

    // Attribute property based order
    [Select(Order = "Name DESC")]
    List<DataEntity> SelectListCustomOrder();

    // ORDER BY /*# order */
    [Select]
    List<DataEntity> SelectParameterOrder([Order] string order);

    //  map to other entity

    // SQL is generated based on DataEntity and map to OtherEntity
    [Select(typeof(DataEntity))]
    List<OtherEntity> SelectListByType();

    // SQL is generated with table name 'Data' and map to OtherEntity
    [Select("Data")]
    List<OtherEntity> SelectListByName();
}
```

* SelectSingleAttribute

```csharp
[DataAccessor]
public interface ISelectAccessor
{
    // Id = /*@ id */
    [SelectSingle]
    DataEntity SelectSingle(long id);

    // By entity key memember
    [SelectSingle]
    DataEntity SelectSingle(DataEntity entity);
}
```

* CountAttribute

```csharp
[DataAccessor]
public interface ICountAccessor
{
    // Count all
    [Count(typeof(DataEntity))]
    long CountAll();

    // Count where Value >= /*@ value */
    [Count(typeof(DataEntity))]
    long CountAll([Condition(Operand.GreaterEqualThan)] long value);
}
```

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
public interface IProcedureAccessor
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

### Condition attribute

```csharp
// Generate condition

// Kye >= /*@ key */
[Delete]
int Delete([Condition(Operand.GreaterEqualThan)] long key);

// /*% if (IsNotNull(type)) { %//*@ type *//*% } */
[Select]
List<DataEntity> Select([Condition(ExcludeNull = true)] string type);

// /*% if (IsNotEmpty(type)) { %//*@ type *//*% } */
[Select]
List<DataEntity> Select([Condition(ExcludeEmpty = true)] string typel);
```

### Value attribute

* DbValueAttribute

```csharp
public class DbValueEntity
{
    [Key]
    public long Id { get; set; }

    // DB value CURRENT_TIMESTAMP is used
    [DbValue("CURRENT_TIMESTAMP")]
    public string DateTime { get; set; }
}
```

* CodeValueAttribute

```csharp
public class DataEntity
{
    [Key]
    public string Key { get; set; }

    // Code counter.Next() is used
    [CodeValue("counter.Next()")]
    public long Value { get; set; }
}
```

```csharp
[DataAccessor]
[Inject(typeof(Counter), "counter")]
public interface ICodeValueAccessor
{
    [Insert]
    void Insert(DataEntity entity);
}
```

### Option builders

Support database specific UPSERT, SELECT FOR UPDATE, etc.

| Package                                   | Database   |
|-------------------------------------------|------------|
| Usa.Smart.Data.Accessor.Options.SqlServer | SQL Server |
| Usa.Smart.Data.Accessor.Options.MySql     | MySQL      |
| Usa.Smart.Data.Accessor.Options.Postgres  | PostgreSQL |

## Special arguments

### DbConnection

```csharp
[DataAccessor]
public interface IDbConnectionAccessor
{
    // DbConnection con is used insted of default IDbProvider connection
    [Execute]
    int Execute(DbConnection con);
}
```

### DbTransaction

```csharp
[DataAccessor]
public interface ITransactionAccessor
{
    // DbTransaction tx is used as transaction and connection
    [Execute]
    int Execute(DbTransaction tx, long id, string name);
}
```

```csharp
using (var tx = con.BeginTransaction())
{
    var effect = accessor.Execute(tx, 1L, "xxx");

    tx.Commit();
}
```

### CancellationToken

```csharp
[DataAccessor]
public interface IExecuteCancelAsyncAccessor
{
    // Cancelable async method
    [Execute]
    ValueTask<int> ExecuteAsync(CancellationToken cancel);
}
```

## Configuration

ExecuteEngineConfig configuration.

### IDbProvider

```csharp
// Default IDbProvider configuration
var engine = new ExecuteEngineConfig()
    .ConfigureComponents(c => c.Add<IDbProvider>(new DelegateDbProvider(() => new SqlConnection(ConnectionString))))
    .ToEngine();
```

```csharp
// Use multiple provider
config.ConfigureComponents(c =>
{
    var selector = new NamedDbProviderSelector();
    selector.AddProvider("Main", new DelegateDbProvider(() => new SqlConnection(MainConnectionString)));
    selector.AddProvider("Sub", new DelegateDbProvider(() => new SqlConnection(SubConnectionString)));
    c.Add<IDbProviderSelector>(selector);
});
```

### Type map

```csharp
// Use DbType.AnsiString for string
config.ConfigureTypeMap(map => map[typeof(string)] = DbType.AnsiString);
```

### Type handler

```csharp
public sealed class DateTimeTickTypeHandler : ITypeHandler
{
    public void SetValue(DbParameter parameter, object value)
    {
        parameter.DbType = DbType.Int64;
        parameter.Value = ((DateTime)value).Ticks;
    }

    public Func<object, object> CreateParse(Type type)
    {
        return x => new DateTime((long)x);
    }
}
```

```csharp
// In database, store DateTime using bigint
config.ConfigureTypeHandlers(handlers => handlers[typeof(DateTime)] = new DateTimeTickTypeHandler());
```

### Result mapper factory

```csharp
// Implement custom result mapper factory
public interface IResultMapperFactory
{
    bool IsMatch(Type type);

    Func<IDataRecord, T> CreateMapper<T>(IResultMapperCreateContext context, Type type, ColumnInfo[] columns);
}
```

```csharp
// Use custom result mapper factory
config.ConfigureResultMapperFactories(mappers => mappers.Add(new CustomResultMapperFactory));
```

## ASP.NET Core integration

```csharp
services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection("Data Source=test.db")));

services.AddDataAccessor(config =>
{
    config.AccessorAssemblies.Add(Assembly.GetExecutingAssembly());
});
```

```csharp
private readonly ISampleAccessor sampleAccessor;

public HomeController(ISampleAccessor sampleAccessor)
{
    this.sampleAccessor = sampleAccessor;
}
```

## Code generation

### Build option

* SmartDataAccessorGeneratorOption

```xml
<PropertyGroup>
  <SmartDataAccessorGeneratorOption>EntityClassSuffix=Model,Entity;FieldNaming=Pascal</SmartDataAccessorGeneratorOption>
</PropertyGroup>
```

| Option            | Description                        |
|-------------------|------------------------------------|
| EntityClassSuffix | Class suffix to convert table name |
| FieldNaming       | Naming rule to convert column name |

### Generated source

Generated source is created at `$(ProjectDir)$(IntermediateOutputPath)SmartDataAccessor`.

## Benchmark (for reference purpose only)

|                    Method |       Mean |      Error |    StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|-------------------------- |-----------:|-----------:|----------:|-------:|------:|------:|----------:|
|             DapperExecute |   399.3 ns |  2.1018 ns |  3.146 ns | 0.1101 |     - |     - |     464 B |
|              SmartExecute |   177.5 ns |  1.3176 ns |  1.890 ns | 0.0894 |     - |     - |     376 B |
|       DapperExecuteScalar |   154.9 ns |  1.3411 ns |  2.007 ns | 0.0362 |     - |     - |     152 B |
|        SmartExecuteScalar |   108.6 ns |  0.9663 ns |  1.446 ns | 0.0362 |     - |     - |     152 B |
|     DapperQueryBufferd100 | 4,742.0 ns | 36.4373 ns | 53.409 ns | 1.3885 |     - |     - |    5856 B |
|      SmartQueryBufferd100 | 3,434.9 ns | 21.0124 ns | 29.457 ns | 1.3199 |     - |     - |    5552 B |
| DapperQueryFirstOrDefault |   434.9 ns |  6.0099 ns |  8.619 ns | 0.1030 |     - |     - |     432 B |
|  SmartQueryFirstOrDefault |   302.6 ns |  1.7062 ns |  2.447 ns | 0.0758 |     - |     - |     320 B |

## Example Project

* [Console example](https://github.com/usausa/Smart-Net-Data-Accessor/tree/master/Example.ConsoleApplication)
* [Web example](https://github.com/usausa/Smart-Net-Data-Accessor/tree/master/Example.WebApplication)
* [Web example with Smart.Resolver](https://github.com/usausa/Smart-Net-Data-Accessor/tree/master/Example.WebApplication2)

## TODO

* Enhanced Result mapper(constructor argument, tuple). (ver 1.0+)
* Enhanced handling of dynamic parameters by roslyn. (ver 1.0+)
