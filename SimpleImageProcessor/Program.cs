using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.IO;

namespace SimpleImageProcessor
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).UseSerilog((context, config) =>
            {
                if (context.HostingEnvironment.IsDevelopment())
                {
                    config.WriteTo.Console();
                }
                else
                {
                    config.WriteTo.File(
                        path: Path.Combine("logs", "log.txt"),
                        restrictedToMinimumLevel: LogEventLevel.Warning,
                        rollingInterval: RollingInterval.Day
                    );
                }
            }).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
