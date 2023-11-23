using Microsoft.Data.Sqlite;

using Smart.Data;
using Smart.Data.Accessor.Extensions.DependencyInjection;
using Smart.Data.Mapper;

#pragma warning disable CA1852

// Configure builder
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Data access
builder.Services.AddSingleton<IDbProvider>(new DelegateDbProvider(static () => new SqliteConnection("Data Source=test.db")));
builder.Services.AddDataAccessor();

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
var dbProvider = app.Services.GetRequiredService<IDbProvider>();
dbProvider.Using(con =>
{
    con.Execute("CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text, Type text)");
    con.Execute("DELETE FROM Data");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (1, 'Data-1', 'A')");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (2, 'Data-2', 'B')");
    con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (3, 'Data-3', 'A')");
});

app.Run();
