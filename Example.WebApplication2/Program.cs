namespace Example.WebApplication2
{
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;

    using Smart.Resolver;

    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddSmartResolver())
                .UseStartup<Startup>();
    }
}
