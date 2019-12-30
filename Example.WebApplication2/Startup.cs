namespace Example.WebApplication2
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Data.Sqlite;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;

    using Smart.Data;
    using Smart.Data.Accessor.Resolver;
    using Smart.Data.Mapper;
    using Smart.Resolver;

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
        }

        public void ConfigureContainer(ResolverConfig config)
        {
            config.UseDataAccessor();
            config
                .Bind<IDbProvider>()
                .ToConstant(new DelegateDbProvider(() => new SqliteConnection("Data Source=primary.db")))
                .Named(DataSource.Primary);
            config
                .Bind<IDbProvider>()
                .ToConstant(new DelegateDbProvider(() => new SqliteConnection("Data Source=secondary.db")))
                .Named(DataSource.Secondary);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IDbProviderSelector selector)
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
        }
    }
}
