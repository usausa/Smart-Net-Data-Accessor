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
            failed += RunWide(connectionString);

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

    // D-4: 閾値超え(ハッシュ switch)の序数解決を実プロバイダで検証する。SELECT は大小違いの別名
    // (ID / USER_名前 / LAST_MODIFIED_BY_X)・未マップ列(extra_unmapped)・無別名式列(裸の 1)・宣言と異なる
    // 列順を含み、衝突バケット(t1_value / t2_value)と混在キー(user_名前 / col_дата)を同時に通す。
    // InvariantGlobalization=true の NativeAOT でも OrdinalIgnoreCase の簡易ケースフォールドが全 Unicode で
    // 効くこと(.NET 5 以降)の実地確認を兼ねる。
    // D-4: verifies the above-threshold (hash-switch) ordinal resolution against the real provider. The SELECT mixes
    // case-variant aliases (ID / USER_名前 / LAST_MODIFIED_BY_X), an unmapped column (extra_unmapped), an unaliased
    // expression column (a bare 1) and a non-declaration column order, driving the collision bucket
    // (t1_value / t2_value) and the mixed keys (user_名前 / col_дата) at once. Doubles as a field check that
    // OrdinalIgnoreCase simple case folding covers full Unicode under InvariantGlobalization=true NativeAOT (.NET 5+).
    private static int RunWide(string connectionString)
    {
        var factory = new DataAccessorFactoryBuilder()
            .UseDbProvider(new DelegateDbProvider(() => new SqliteConnection(connectionString)))
            .Build();
        var accessor = factory.Create<AotAccessor>();
        var rows = accessor.QueryWide();

        var ok = (rows.Count == 1) &&
                 (rows[0].Id == 1L) &&
                 (rows[0].Age == 30) &&
                 (rows[0].City == "Tokyo") &&
                 (rows[0].Email == "a@example.com") &&
                 (rows[0].Status == 5) &&
                 (rows[0].UserName == "山田") &&
                 (rows[0].ColDate == "2026-01-01") &&
                 (rows[0].ItemCode == "C-1") &&
                 (rows[0].CreatedAt == "2026-07-19") &&
                 (rows[0].StatusCode == "SC") &&
                 (rows[0].DisplayName == "Alice Smith") &&
                 (rows[0].DepartmentId == 10) &&
                 (rows[0].AddressLine1 == "Chiyoda 1-1") &&
                 (rows[0].ManagerUserId == 99) &&
                 (rows[0].OrganizationCd1 == "ORG") &&
                 (rows[0].RegistrationDate == "2020-04-01") &&
                 (rows[0].LastModifiedByX == "admin") &&
                 (rows[0].T1Value == 21) &&
                 (rows[0].T2Value == 22);
        Console.WriteLine($"  [{(ok ? "OK" : "NG")}] D-4 wide switch: {rows.Count} row(s)");
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
            "INSERT INTO Data (Name, Type) VALUES ('Alice', 1), ('Bob', 2), ('Carol', 1);" +
            "CREATE TABLE IF NOT EXISTS Wide (" +
            "id INTEGER PRIMARY KEY, age INTEGER NOT NULL, city TEXT NOT NULL, email TEXT NOT NULL, status INTEGER NOT NULL, " +
            "\"user_名前\" TEXT NOT NULL, \"col_дата\" TEXT NOT NULL, item_code TEXT NOT NULL, created_at TEXT NOT NULL, " +
            "status_code TEXT NOT NULL, display_name TEXT NOT NULL, department_id INTEGER NOT NULL, address_line_1 TEXT NOT NULL, " +
            "manager_user_id INTEGER NOT NULL, organization_cd1 TEXT NOT NULL, registration_date TEXT NOT NULL, " +
            "last_modified_by_x TEXT NOT NULL, t1_value INTEGER NOT NULL, t2_value INTEGER NOT NULL, extra_unmapped TEXT NOT NULL);" +
            "DELETE FROM Wide;" +
            "INSERT INTO Wide VALUES (1, 30, 'Tokyo', 'a@example.com', 5, '山田', '2026-01-01', 'C-1', '2026-07-19', 'SC', " +
            "'Alice Smith', 10, 'Chiyoda 1-1', 99, 'ORG', '2020-04-01', 'admin', 21, 22, 'x');";
        command.ExecuteNonQuery();
    }
}
