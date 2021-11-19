namespace Example.WebApplication;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Smart.Data;
using Smart.Data.Accessor.Extensions.DependencyInjection;
using Smart.Data.Mapper;

[System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Ignore")]
public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllersWithViews();

        services.AddSingleton<IDbProvider>(new DelegateDbProvider(() => new SqliteConnection("Data Source=test.db")));
        services.AddDataAccessor();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IDbProvider dbProvider)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseEndpoints(routes =>
        {
            routes.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        });

        dbProvider.Using(con =>
        {
            con.Execute("CREATE TABLE IF NOT EXISTS Data (Id int PRIMARY KEY, Name text, Type text)");
            con.Execute("DELETE FROM Data");
            con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (1, 'Data-1', 'A')");
            con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (2, 'Data-2', 'B')");
            con.Execute("INSERT INTO Data (Id, Name, Type) VALUES (3, 'Data-3', 'A')");
        });
    }
}
