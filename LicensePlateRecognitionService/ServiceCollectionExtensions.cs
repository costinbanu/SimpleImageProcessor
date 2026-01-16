using System;
using CSnakes.Runtime;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace LicensePlateRecognitionService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLicensePlateRecognitionService(this IServiceCollection services, IHostEnvironment environment)
    {
        services.WithPython()
            .WithHome(environment.ContentRootPath)
            .WithVirtualEnvironment("pythonVirtualEnvironment")
            .WithUvInstaller()
            .FromRedistributable();

        services.AddScoped<ILPRService, LPRService>();

        return services;
    }
}