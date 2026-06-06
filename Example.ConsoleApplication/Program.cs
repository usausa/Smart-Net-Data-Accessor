namespace Example.ConsoleApplication;

using Example.ConsoleApplication.Accessor;
using Example.ConsoleApplication.Models;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Smart.Data;

internal static class Program
{
    public static int Main()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-data-accessor-example-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            var failed = 0;
            failed += RunPattern1(connectionString);
            failed += RunPattern2(connectionString);
            failed += RunPattern3(connectionString);
            failed += RunConverterSample(connectionString);
            return failed == 0 ? 0 : 1;
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(dbPath))
            {
                File.Delete(dbPath);
            }
        }
    }

    // Pattern 1: DelegateDbProvider (single source, fresh connection per accessor call).
    private static int RunPattern1(string connectionString)
    {
        Console.WriteLine("=== Pattern 1: DelegateDbProvider ===");

        var accessor = new ExampleAccessor(
            new DelegateDbProvider(() => new SqliteConnection(connectionString)));

        accessor.Create();

        accessor.Insert(new DataEntity { Name = "Alice", Type = 1, Kind = DataType.Small });
        accessor.Insert(new DataEntity { Name = "Bob", Type = 2, Kind = DataType.Large });
        accessor.Insert(new DataEntity { Name = "Carol", Type = 3, Kind = DataType.Large });

        var list = accessor.QueryDataList();
        foreach (var item in list)
        {
            Console.WriteLine($"  Id={item.Id}, Name={item.Name}, Type={item.Type}, Kind={item.Kind}");
        }

        var type2 = accessor.QueryByType(2);
        Console.WriteLine($"QueryByType(2) -> {type2.Count} row(s)");

        var byKind = accessor.QueryByKind(DataType.Large);
        Console.WriteLine($"QueryByKind(Large) -> {byKind.Count} row(s)");

        var ids = new List<long>();
        foreach (var item in list)
        {
            ids.Add(item.Id);
        }
        var byIds = accessor.QueryByIds(ids);
        Console.WriteLine($"QueryByIds([{String.Join(",", ids)}]) -> {byIds.Count} row(s)  // IN expansion");

        var emptyIds = accessor.QueryByIds(Array.Empty<long>());
        Console.WriteLine($"QueryByIds([]) -> {emptyIds.Count} row(s)  // empty IN -> '(NULL)'");

        var records = accessor.QueryAllAsRecord();
        Console.WriteLine($"QueryAllAsRecord -> {records.Count} record(s)  // record primary ctor");

        var readerRows = 0;
        using (var reader = accessor.QueryReader())
        {
            while (reader.Read())
            {
                readerRows++;
            }
        }
        Console.WriteLine($"QueryReader -> {readerRows} row(s)  // raw DbDataReader");

        var selectAll = accessor.SelectAll();
        Console.WriteLine($"SelectAll -> {selectAll.Count} row(s)");

        var upd = new DataEntity { Id = selectAll[0].Id, Name = "Alice2", Type = 11, Kind = DataType.Unknown };
        var updated = accessor.UpdateEntity(upd);
        Console.WriteLine($"UpdateEntity -> {updated} row(s) updated");

        var deleted = accessor.DeleteById(selectAll[2].Id);
        Console.WriteLine($"DeleteById -> {deleted} row(s) deleted");

        var count = accessor.CountAll();
        Console.WriteLine($"CountAll -> {count}");

        Console.WriteLine();
        var ok = (list.Count == 3)
            && (type2.Count == 1)
            && (byKind.Count == 2)
            && (byIds.Count == 3)
            && (emptyIds.Count == 0)
            && (records.Count == 3)
            && (readerRows == 3)
            && (updated == 1)
            && (deleted == 1)
            && (count == 2);
        return ok ? 0 : 1;
    }

    // Pattern 2: caller-managed DbConnection / DbTransaction passed directly to methods.
    private static int RunPattern2(string connectionString)
    {
        Console.WriteLine("=== Pattern 2: Direct DbConnection / DbTransaction args ===");

        using var connection = new SqliteConnection(connectionString);
        connection.Open();

        var accessor = new ExampleAccessor(
            new DelegateDbProvider(() => new SqliteConnection(connectionString)));

        var all = accessor.QueryAllWithConnection(connection);
        Console.WriteLine($"QueryAllWithConnection -> {all.Count} row(s)");

        var beforeCount = accessor.CountAll();

        accessor.InsertNameByConnection(connection, "DaveFromConn", 7);

        using (var tx = connection.BeginTransaction())
        {
            accessor.InsertNameByTransaction(tx, "EveFromTx", 8);
            tx.Commit();
        }

        var afterCount = accessor.CountAll();
        Console.WriteLine($"CountAll: {beforeCount} -> {afterCount}");

        Console.WriteLine();
        return afterCount == beforeCount + 2 ? 0 : 1;
    }

    // Pattern 3: M.E.DI integration via AddDataAccessors() + [Inject] sample.
    private static int RunPattern3(string connectionString)
    {
        Console.WriteLine("=== Pattern 3: Microsoft.Extensions.DependencyInjection ===");

        var counter = new ConsoleLogger();

        var services = new ServiceCollection();
        services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
        services.AddSingleton<IExampleLogger>(counter);
        services.AddDataAccessors();

        using var sp = services.BuildServiceProvider();

        var accessor = sp.GetRequiredService<ExampleAccessor>();
        var rows = accessor.SelectAll();
        Console.WriteLine($"DI ExampleAccessor.SelectAll -> {rows.Count} row(s)");

        var injectAccessor = sp.GetRequiredService<ExampleInjectAccessor>();
        var logged = injectAccessor.CallLoggerAndCount("hello from DI");
        var all = injectAccessor.QueryAll();
        Console.WriteLine($"DI ExampleInjectAccessor.QueryAll -> {all.Count} row(s), logger={injectAccessor.GetLoggerTypeName()}, count={logged}");

        Console.WriteLine();
        return (all.Count == rows.Count) && (logged == 1) ? 0 : 1;
    }

    // Custom [TypeHandler] sample (F6): a DateTime property stored as Int64 ticks, verified round-trip.
    private static int RunConverterSample(string connectionString)
    {
        Console.WriteLine("=== Custom TypeHandler: DateTime <-> Int64 ticks ===");

        var accessor = new ExampleAccessor(
            new DelegateDbProvider(() => new SqliteConnection(connectionString)));

        accessor.CreateEvents();

        var occurred = new DateTime(2026, 6, 2, 8, 30, 0, DateTimeKind.Utc);
        accessor.InsertEvent(new EventEntity { OccurredAt = occurred });

        var events = accessor.QueryEvents();
        var roundTripped = (events.Count == 1)
            && (events[0].OccurredAt == occurred)
            && (events[0].OccurredAt.Kind == DateTimeKind.Utc);
        Console.WriteLine($"InsertEvent/QueryEvents -> {events.Count} row(s), OccurredAt={events[0].OccurredAt:O} (ToDb/FromDb round-trip {(roundTripped ? "OK" : "FAIL")})");

        Console.WriteLine();
        return roundTripped ? 0 : 1;
    }

    private sealed class ConsoleLogger : IExampleLogger
    {
        public int Count { get; private set; }

        public void Log(string message)
        {
            Count++;
            Console.WriteLine($"  [Logger#{Count}] {message}");
        }
    }
}
