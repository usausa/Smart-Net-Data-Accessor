namespace Example.WebApplication2;

using Example.WebApplication2.Accessor;

using Microsoft.Data.Sqlite;

using Smart.Data;
using Smart.Data.Accessor.Resolver;
using Smart.Resolver;

// Minimal-API sample: two data sources selected by key, wired through Smart.Resolver (Phase 4).
// SmartServiceProviderFactory makes Smart.Resolver the host container; UseDataAccessors() binds the
// generator-discovered accessors and an IDbProviderSelector backed by the resolver. A
// [Provider("Primary")] / [Provider("Secondary")] accessor resolves its keyed IDbProvider.
internal static class Program
{
    public static void Main(string[] args)
    {
        var primaryConnectionString = $"Data Source={Path.Combine(Path.GetTempPath(), "smart-data-accessor-webapp2-primary.db")}";
        var secondaryConnectionString = $"Data Source={Path.Combine(Path.GetTempPath(), "smart-data-accessor-webapp2-secondary.db")}";

        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseServiceProviderFactory(new SmartServiceProviderFactory());
        builder.Host.ConfigureContainer<ResolverConfig>(config =>
        {
            config.UseDataAccessors();
            config.Bind<IDbProvider>()
                .ToConstant(new DelegateDbProvider(() => new SqliteConnection(primaryConnectionString)))
                .Keyed(DataSource.Primary);
            config.Bind<IDbProvider>()
                .ToConstant(new DelegateDbProvider(() => new SqliteConnection(secondaryConnectionString)))
                .Keyed(DataSource.Secondary);
        });

        var app = builder.Build();

        var selector = app.Services.GetRequiredService<IDbProviderSelector>();
        SeedDatabase(selector.GetProvider(DataSource.Primary), "Primary");
        SeedDatabase(selector.GetProvider(DataSource.Secondary), "Secondary");

        var primary = app.Services.GetRequiredService<PrimaryAccessor>();
        var secondary = app.Services.GetRequiredService<SecondaryAccessor>();

        app.MapGet("/", static () => "Example.WebApplication2 (Smart.Resolver, multi-source). GET /primary or /secondary.");
        app.MapGet("/primary", primary.QueryAll);
        app.MapGet("/secondary", secondary.QueryAll);

        app.Run();
    }

    private static void SeedDatabase(IDbProvider provider, string prefix)
    {
        using var connection = provider.CreateConnection();
        connection.Open();

        using (var create = connection.CreateCommand())
        {
            create.CommandText =
                "CREATE TABLE IF NOT EXISTS Data (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL, Type INTEGER NOT NULL);" +
                "DELETE FROM Data;";
            create.ExecuteNonQuery();
        }

        // Parameterized insert (the source label flows through a parameter value, not the SQL text).
        for (var i = 1; i <= 3; i++)
        {
            using var insert = connection.CreateCommand();
            insert.CommandText = "INSERT INTO Data (Name, Type) VALUES (@name, @type)";
            var name = insert.CreateParameter();
            name.ParameterName = "@name";
            name.Value = $"{prefix}-{i}";
            insert.Parameters.Add(name);
            var type = insert.CreateParameter();
            type.ParameterName = "@type";
            type.Value = i;
            insert.Parameters.Add(type);
            insert.ExecuteNonQuery();
        }
    }
}
