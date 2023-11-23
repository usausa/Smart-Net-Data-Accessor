using Example.WebApplication2;

using Microsoft.Data.Sqlite;

using Smart.Data;
using Smart.Data.Accessor.Extensions.DependencyInjection;
using Smart.Data.Accessor.Resolver;
using Smart.Data.Mapper;
using Smart.Resolver;

#pragma warning disable CA1852

// Configure builder
var builder = WebApplication.CreateBuilder(args);

// Custom service provider
builder.Host.UseServiceProviderFactory(new SmartServiceProviderFactory());

// Add services to the container.
builder.Services.AddControllersWithViews();

// Data access
builder.Services.AddSingleton<IDbProvider>(new DelegateDbProvider(static () => new SqliteConnection("Data Source=test.db")));
builder.Services.AddDataAccessor();

// Custom service provider
builder.Host.ConfigureContainer<ResolverConfig>(config =>
{
    config.UseDataAccessor();
    config
        .Bind<IDbProvider>()
        .ToConstant(new DelegateDbProvider(static () => new SqliteConnection("Data Source=primary.db")))
        .Named(DataSource.Primary);
    config
        .Bind<IDbProvider>()
        .ToConstant(new DelegateDbProvider(static () => new SqliteConnection("Data Source=secondary.db")))
        .Named(DataSource.Secondary);
});

// Configure
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Data initialize
var selector = app.Services.GetRequiredService<IDbProviderSelector>();

var provider1 = selector.GetProvider(DataSource.Primary);
provider1.Using(con =>
{
    con.Execute("CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text, Type text)");
    con.Execute("DELETE FROM Data");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (1, 'Primary-1', 'A')");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (2, 'Primary-2', 'B')");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (3, 'Primary-3', 'A')");
});

var provider2 = selector.GetProvider(DataSource.Secondary);
provider2.Using(con =>
{
    con.Execute("CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text, Type text)");
    con.Execute("DELETE FROM Data");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (1, 'Secondary-1', 'B')");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (2, 'Secondary-2', 'A')");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (3, 'Secondary-3', 'B')");
});

app.Run();
