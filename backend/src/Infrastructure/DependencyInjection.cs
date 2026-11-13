using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NasaMoon.Application.Interfaces;
using NasaMoon.Infrastructure.Repositories;

namespace NasaMoon.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NasaMoonDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IMissionRepository, MissionRepository>();
        services.AddScoped<IPayloadRepository, PayloadRepository>();
        services.AddScoped<ISourceRepository, SourceRepository>();

        return services;
    }
}
