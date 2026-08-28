# Smart.Data.Accessor - Source Generator based data accessor for .NET

[![NuGet](https://img.shields.io/nuget/v/Usa.Smart.Data.Accessor.svg)](https://www.nuget.org/packages/Usa.Smart.Data.Accessor)

## What is this?

A **compile-time data accessor generator**. You declare `partial` methods with attributes and
(optionally) 2-way SQL, and a Roslyn Source Generator emits the full ADO.NET implementation —
connection handling, command setup, parameter binding, and row mapping — as plain, readable C#.

* **Build-time code generation** — no reflection, no IL emit, no runtime proxy. What runs is code you can read.
* **2-way SQL** — SQL files (or inline SQL) that are valid SQL as-is, with comment-based directives for binding and dynamic conditions.
* **Near hand-written performance** — 0.95–1.13x of hand-written ADO.NET, with identical allocations; 1.4–2.2x faster than Dapper (see [Benchmarks](#benchmarks)).
* **Native AOT compatible** — the generated code is fully static.
* **Compile-time diagnostics** — misuse (missing SQL file, invalid return type, unmappable entity, conflicting attributes, broken 2-way SQL, ...) fails the build with a precise `SDA` diagnostic instead of a runtime surprise.

## Getting Started (Console Application)

Install [Usa.Smart.Data.Accessor](https://www.nuget.org/packages/Usa.Smart.Data.Accessor).

Define a model and a data accessor. The accessor is a `partial class` marked `[DataAccessor]`;
each data method is a `partial` method marked with an **execution-kind attribute**
(`[Execute]` / `[ExecuteScalar]` / `[Query]` / `[QueryFirst]` / `[ExecuteReader]`).

```csharp
public sealed class DataEntity
{
    public long Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Type { get; set; }
}
```

```csharp
using Smart.Data.Accessor.Attributes;

[DataAccessor]
public partial class ExampleAccessor
{
    [Execute]
    public partial int Create();

    [Query]
    public partial IReadOnlyList<DataEntity> QueryDataList();

    [Query]
    public partial IReadOnlyList<DataEntity> QueryByType(int type);
}
```

Add the SQL files under a `Sql` folder, named `{ClassName}.{MethodName}.sql`
(the package wires `**/Sql/*.sql` as generator inputs automatically; the folder name can be
changed with the `SmartDataAccessor_SqlFolder` MSBuild property):

```
Accessor/
  ExampleAccessor.cs
  Sql/
    ExampleAccessor.Create.sql
    ExampleAccessor.QueryDataList.sql
    ExampleAccessor.QueryByType.sql
```

```sql
-- ExampleAccessor.QueryByType.sql
SELECT Id, Name, Type FROM Data
WHERE Type = /*@ type */1
ORDER BY Id
```

Use it. Without a `DbConnection` parameter, the accessor takes an `IDbProvider`
(from [Usa.Smart.Data](https://www.nuget.org/packages/Usa.Smart.Data)) in its constructor and
manages open/close per call:

```csharp
using Smart.Data;

var accessor = new ExampleAccessor(
    new DelegateDbProvider(() => new SqliteConnection(connectionString)));

accessor.Create();
var list = accessor.QueryByType(1);
```

That is all — no configuration classes, no runtime setup. Everything is resolved at build time.

## 2-way SQL

The SQL files are **valid SQL as-is** (they run unchanged in your SQL tools); directives live in comments:

| Directive | Meaning |
| --- | --- |
| `/*@ name */dummy` | Bind the method parameter `name`. The literal after the comment is a placeholder for tooling and is replaced at build time |
| `/*% if (cond) { */ ... /*% } */` | Dynamic block — the condition is real C# over the method parameters, evaluated at runtime with zero string parsing |
| `/*@ ids */(...)` | An `IEnumerable<T>` parameter expands to an IN list at runtime (empty lists become `(NULL)`) |
| `/*!using Ns */`, `/*!helper Type */` | Add `using` / `using static` to the generated file so the `if` conditions can call helpers |

```sql
SELECT * FROM Data
/*% if (name != null) { */
WHERE Name LIKE /*@ name */'A%'
/*% } */
ORDER BY Id
```

Static SQL (no dynamic blocks) is embedded as a single string literal — no `StringBuilder`, no runtime work.

### Inline SQL — `[Sql]`

Short queries can skip the file and put the 2-way SQL directly on the method
(same pipeline and directives; C# 11 raw string literals work well for multi-line SQL).
SQL parse errors point at the exact position inside the literal.

```csharp
[Query]
[Sql("SELECT * FROM Data WHERE Type >= /*@ minType */0 ORDER BY Id")]
public partial IReadOnlyList<DataEntity> QueryByTypeInline(int minType);
```

## Method model: execution kind × command source

Every data method combines two orthogonal choices:

* **Execution kind (required)** — how the command runs: `[Execute]` (ExecuteNonQuery),
  `[ExecuteScalar]`, `[Query]` (rows), `[QueryFirst]` (single row or null), `[ExecuteReader]` (raw reader).
* **Command source (optional)** — where the SQL comes from: SQL file (default), `[Sql("...")]` inline,
  `[DirectSql]` (runtime SQL string parameter), `[Procedure("name")]` (stored procedure),
  or a QueryBuilder attribute (`[Insert]` etc.).

Combinations compose naturally (`[Query]` + `[Procedure]`, `[ExecuteReader]` + `[DirectSql]`, ...).
The execution kind is never implied — omitting it is a compile-time error (SDA0108).

Supported return shapes include `int` / `void`, scalars, `List<T>` / `IList<T>` / `IReadOnlyList<T>`,
`IEnumerable<T>` (streaming iterator), `T?` (single row), `DbDataReader`, and the async forms
(`Task<...>` / `ValueTask<...>` / `IAsyncEnumerable<T>`).

## Row mapping

Rows map to plain classes or records by **case-insensitive column-name matching**, resolved once per
query (not per row):

* Columns are matched to public settable/init properties (or record primary-constructor parameters).
  `[Name("COL")]` overrides the name; `[Ignore]` excludes a member.
* `[Naming(NamingConvention.SnakeCaseLower)]` (method/class/assembly scope, like `[BindPrefix]`)
  converts the default names instead of annotating every property — `UserId` matches `user_id`
  without a `[Name]`. An explicit `[Name]` always wins.
* **Subset selects just work** — properties without a matching column are left untouched
  (property initializers survive). Extra columns are ignored. Dynamic column sets (UNION etc.) are fine.
* `records`, `init`-only and `required` members are fully supported.
* DB NULL maps to `null` for nullable members and `default` for non-nullable ones.
* `[NotNullColumn]` skips the per-column `IsDBNull` check for NOT NULL columns (hot-path opt-in).
* `[TypeHandler(typeof(Converter))]` maps custom representations via static, AOT-friendly converters:

```csharp
public sealed class DateTimeToTicksConverter : IValueConverter<long, DateTime>
{
    public static DateTime FromDb(long value) => new(value, DateTimeKind.Utc);
    public static long ToDb(DateTime value) => value.Ticks;
}

public sealed class EventEntity
{
    public long Id { get; set; }

    [TypeHandler(typeof(DateTimeToTicksConverter))]
    public DateTime OccurredAt { get; set; }
}
```

## Query builders

CRUD without writing SQL — the builder attributes generate the statement from the entity shape
(`[Key]`, `[Name]`, `[DatabaseManaged]`, `[Ignore]`):

```csharp
[Insert(typeof(DataEntity), Table = "Data")]
[Execute]
public partial int Insert(DataEntity entity);

[Select(typeof(DataEntity), Table = "Data")]
[Query]
public partial IReadOnlyList<DataEntity> SelectAll();

[Count(typeof(DataEntity), Table = "Data")]
[ExecuteScalar]
public partial long CountAll();
```

Standard (ANSI) builders (`[Insert]` / `[Update]` / `[Delete]` / `[Count]` / `[Select]` /
`[SelectSingle]` / `[Truncate]`) ship in the core package; `[Limit]` / `[Offset]` parameters give
paging with the proper dialect per provider. `[Naming(...)]` converts the default table name
(the entity type name) and the default column names to `snake_case` etc. (`Table =` / `[Name]`
still win). Provider packages add dialect features:

| Package | Attributes | Extras |
| --- | --- | --- |
| `Usa.Smart.Data.Accessor.Builders.SqlServer` | `SqlInsert`, ... , `SqlMerge` | `OUTPUT` clause, MERGE upsert |
| `Usa.Smart.Data.Accessor.Builders.Postgres` | `PgInsert`, ... , `PgUpsert` | `RETURNING` clause, `ON CONFLICT` upsert |
| `Usa.Smart.Data.Accessor.Builders.MySql` | `MySqlInsert`, ... , `MySqlUpsert`, `MySqlInsertIgnore`, `MySqlReplace` | `ON DUPLICATE KEY UPDATE`, `INSERT IGNORE`, `REPLACE INTO` |

Writing a builder generator for another provider is supported and documented
([`docs/generator-guide.md`](docs/generator-guide.md), Japanese: [`generator-guide.ja.md`](docs/generator-guide.ja.md)).

## Stored procedures and output parameters

```csharp
[Procedure("usp_Calc")]
[ExecuteScalar]
public partial int Calc(DbConnection con, CalcArgs args);   // scalar return = procedure RETURN value

[Procedure("usp_Conv")]
[Execute]
public partial void Conv(DbConnection con, ConvArgs args);
```

Parameters map by name; `out` / `ref` parameters (sync) or `[Direction(Output/InputOutput)]`
properties on a POCO argument (sync and async) receive the output values.

## Raw SQL and raw readers

```csharp
// SQL passed at runtime (explicit opt-in; injection safety is the caller's responsibility)
[DirectSql]
[Execute]
public partial int ExecuteDirect(string sql, [Direction(ParameterDirection.Output)] out int rows);

// Raw reader; CommandBehavior can be opted in (the caller controls the read order,
// so SequentialAccess is safe here for large BLOB/TEXT streaming)
[ExecuteReader]
[ReaderBehavior(CommandBehavior.SequentialAccess)]
public partial DbDataReader QueryReader();
```

`[ExecuteReader]` returns a wrapper that disposes the command (and, for provider-owned
connections, the connection) together with the reader — a single `using` on the caller side.

## Connections and DI

Two patterns, chosen per method by the signature:

* **Pattern A** — the method takes a `DbConnection` (or `DbTransaction`); you own the connection.
* **Pattern B** — no connection parameter; the accessor gets an `IDbProvider`
  (or `IDbProviderSelector` + `[Provider("name")]` for multi-database) via its constructor and
  opens/closes per call.

With `Microsoft.Extensions.DependencyInjection`
([Usa.Smart.Data.Accessor.Extensions.DependencyInjection](https://www.nuget.org/packages/Usa.Smart.Data.Accessor.Extensions.DependencyInjection)):

```csharp
builder.Services.AddSingleton<IDbProvider>(
    new DelegateDbProvider(() => new SqliteConnection(connectionString)));
builder.Services.AddDataAccessors();   // registers every generated accessor

app.MapGet("/data", (ExampleAccessor accessor) => accessor.QueryDataList());
```

When the accessors live in a **separate assembly** (a data-layer library), pass that assembly so
its module initializers run before registration — otherwise the lazy assembly load can leave the
registry empty and `AddDataAccessors()` silently registers nothing:

```csharp
builder.Services.AddDataAccessors(typeof(MyAccessor).Assembly);
```

[Usa.Smart.Data.Accessor.Resolver](https://www.nuget.org/packages/Usa.Smart.Data.Accessor.Resolver)
provides the same for [Usa.Smart.Resolver](https://www.nuget.org/packages/Usa.Smart.Resolver)
(`config.UseDataAccessors()`), including keyed multi-source setups.

## Other attributes

| Attribute | Purpose |
| --- | --- |
| `[MethodName("Alias")]` | SQL-file name alias for same-name overloads |
| `[CommandTimeout(30)]` / `[Timeout(30)]` | `cmd.CommandTimeout` |
| `[DbType(...)]`, `[DbType<TEnum>(...)]`, `[AnsiString]`, `[SqlSize]` | Parameter type/size qualifiers (incl. provider-specific enum types) |
| `[BindPrefix('@')]` | Override the parameter marker per method/class/assembly |
| `[Naming(NamingConvention.SnakeCaseLower)]` | Default-name conversion (snake_case / lower / upper) per method/class/assembly when `[Name]` is absent |
| `[Inject]` | Inject a service into the accessor, usable from SQL `if` conditions |
| `[TypeMap]`, `[AccessorProfile]`, `[ExecuteConfig]` | Class/profile-scoped type mapping defaults |

## Native AOT

The generated code is static (no reflection); publishing with `PublishAot=true` is verified,
including the DI integrations.

## Benchmarks

Mock-connection measurements (mapping layer only, BenchmarkDotNet, .NET 10 / x64), 100 rows,
ratio vs hand-written ADO.NET baseline:

| Scenario | Generated vs hand-written | vs Dapper | Allocations |
| --- | ---: | ---: | --- |
| 1 column | 0.95–0.98x (faster) | ~2.2x faster | identical to hand-written |
| 3 columns (enum) | 0.97–0.99x | ~2.1x faster | identical |
| 10 columns (class/record) | 1.02–1.07x | ~1.4–1.5x faster | identical |
| Subset (10 props / 2 cols) | 1.10–1.13x | ~1.9x faster | identical |

With a real database, network/query time dominates and the differences shrink further
(relative order unchanged).

## Packages

| Package | Contents |
| --- | --- |
| `Usa.Smart.Data.Accessor` | Runtime + core source generator + standard builders |
| `Usa.Smart.Data.Accessor.Extensions.DependencyInjection` | `AddDataAccessors()` for Microsoft.Extensions.DependencyInjection |
| `Usa.Smart.Data.Accessor.Resolver` | `UseDataAccessors()` for Smart.Resolver |
| `Usa.Smart.Data.Accessor.Builders.SqlServer` / `.Postgres` / `.MySql` | Provider-specific query builders |

## Documentation

* [`docs/generator-guide.md`](docs/generator-guide.md) — how to write a third-party provider builder generator (Japanese: [`generator-guide.ja.md`](docs/generator-guide.ja.md))
* `Example.ConsoleApplication` / `Example.WebApplication` — runnable samples (SQLite)

## Verification

Beyond the unit/functional test suites, the following have been verified end to end:

* **Real databases** — the provider builder dialects run against real servers:
  SQL Server (LocalDB: `MERGE` upsert, `OUTPUT`, paging, stored procedures),
  PostgreSQL 17 (`ON CONFLICT DO UPDATE`, `RETURNING`, paging),
  MySQL-compatible MariaDB 11 (`ON DUPLICATE KEY UPDATE`, `INSERT IGNORE`, `REPLACE`, paging),
  and SQLite (samples and functional tests).
* **Native AOT** — native publish + run verified with both `Microsoft.Data.Sqlite` and
  `Microsoft.Data.SqlClient`.
* **Package consumption** — a fresh project referencing only the produced nupkgs
  (no project references) generates and executes correctly, including the transitive
  `.sql` auto-import targets.
* **Generated code compilation** — the generator test harness compiles the generated
  code and fails on any compile error, in addition to text/diagnostic assertions.
