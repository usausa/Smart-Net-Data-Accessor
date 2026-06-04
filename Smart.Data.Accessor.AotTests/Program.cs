namespace Smart.Data.Accessor.AotTests;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Smart.Data.Accessor.Resolver;
using Smart.Resolver;

// NativeAOT smoke test: the generator-produced accessor is exercised through the three DI paths
// (built-in factory / Microsoft.Extensions.DependencyInjection / Smart.Resolver). Returns 0 when
// every path maps the seeded rows, 1 otherwise.
internal static class Program
{
    public static int Main()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"smart-data-accessor-aot-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={dbPath}";

        try
        {
            Seed(connectionString);

            var failed = 0;
            failed += RunBuiltIn(connectionString);
            failed += RunMicrosoftDependencyInjection(connectionString);
            failed += RunResolver(connectionString);

            Console.WriteLine(failed == 0 ? "AOT smoke: ALL PASS" : "AOT smoke: FAILED");
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

    // D-1: built-in container (DataAccessorFactoryBuilder -> DataAccessorFactory.Create<T>()).
    private static int RunBuiltIn(string connectionString)
    {
        var factory = new DataAccessorFactoryBuilder()
            .UseDbProvider(new DelegateDbProvider(() => new SqliteConnection(connectionString)))
            .Build();
        var accessor = factory.Create<AotAccessor>();
        return Report("D-1 built-in", accessor.QueryAll());
    }

    // D-2: Microsoft.Extensions.DependencyInjection (AddDataAccessors -> GetRequiredService<T>()).
    private static int RunMicrosoftDependencyInjection(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
        services.AddDataAccessors();

        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<AotAccessor>();
        return Report("D-2 M.E.DI", accessor.QueryAll());
    }

    // D-3: Smart.Resolver (UseDataAccessors -> resolver.Get<T>()).
    private static int RunResolver(string connectionString)
    {
        var config = new ResolverConfig();
        config.UseDataAccessors();
        config.Bind<IDbProvider>()
            .ToConstant(new DelegateDbProvider(() => new SqliteConnection(connectionString)))
            .InSingletonScope();

        using var resolver = config.ToResolver();
        var accessor = resolver.Get<AotAccessor>();
        return Report("D-3 Resolver", accessor.QueryAll());
    }

    private static int Report(string label, IReadOnlyList<AotData> rows)
    {
        var ok = (rows.Count == 3) && (rows[0].Name == "Alice") && (rows[2].Type == 1);
        Console.WriteLine($"  [{(ok ? "OK" : "NG")}] {label}: {rows.Count} row(s)");
        return ok ? 0 : 1;
    }

    private static void Seed(string connectionString)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE IF NOT EXISTS Data (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Type INTEGER NOT NULL);" +
            "DELETE FROM Data;" +
            "INSERT INTO Data (Name, Type) VALUES ('Alice', 1), ('Bob', 2), ('Carol', 1);";
        command.ExecuteNonQuery();
    }
}
