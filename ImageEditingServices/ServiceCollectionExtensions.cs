using System;
using Microsoft.Extensions.DependencyInjection;

namespace ImageEditingServices;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImageEditingServices(this IServiceCollection services)
    {
        services.AddScoped<IImageResizer, ImageResizer>();
        return services;
    }
}
