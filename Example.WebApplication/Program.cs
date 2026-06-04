namespace Example.WebApplication;

using Example.WebApplication.Accessor;

using Microsoft.AspNetCore.Builder;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;

using Smart.Data;

// Minimal-API sample: a single data source wired through Microsoft.Extensions.DependencyInjection.
// `AddDataAccessors()` registers every generator-discovered accessor; the accessor's IDbProvider
// dependency is resolved from the container (Pattern B).
internal static class Program
{
    public static void Main(string[] args)
    {
        var dbPath = Path.Combine(Path.GetTempPath(), "smart-data-accessor-webapp1.db");
        var connectionString = $"Data Source={dbPath}";

        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection(connectionString)));
        builder.Services.AddDataAccessors();

        var app = builder.Build();

        SeedDatabase(app.Services.GetRequiredService<IDbProvider>());

        var accessor = app.Services.GetRequiredService<WebDataAccessor>();

        app.MapGet("/", static () => "Example.WebApplication (M.E.DI, single source). GET /data for rows.");
        app.MapGet("/data", () => accessor.QueryAll());

        app.Run();
    }

    private static void SeedDatabase(IDbProvider provider)
    {
        using var connection = provider.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "CREATE TABLE IF NOT EXISTS Data (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Type INTEGER NOT NULL);" +
            "DELETE FROM Data;" +
            "INSERT INTO Data (Name, Type) VALUES ('Alice', 1), ('Bob', 2), ('Carol', 1);";
        command.ExecuteNonQuery();
    }
}
