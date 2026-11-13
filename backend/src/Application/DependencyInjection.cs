using Microsoft.Extensions.DependencyInjection;
using NasaMoon.Application.Interfaces;
using NasaMoon.Application.Services;

namespace NasaMoon.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMissionService, MissionService>();
        services.AddScoped<IPayloadService, PayloadService>();
        return services;
    }
}
