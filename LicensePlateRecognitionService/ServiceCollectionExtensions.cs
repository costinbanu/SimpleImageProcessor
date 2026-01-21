using System;
using System.IO;
using System.Reflection;
using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LicensePlateRecognitionService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLicensePlateRecognitionService(this IServiceCollection services)
    {
        var home = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        services.WithPython()
            .WithHome(home)
            .WithVirtualEnvironment(Path.Combine(home, "pythonVirtualEnvironment"))
            .WithUvInstaller()
            .FromRedistributable()
            .CapturePythonLogs();

        services.AddScoped<ILPRService, LPRService>();

        return services;
    }
}