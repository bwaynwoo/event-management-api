using EventService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, Services.EventService>();

        return services;
    }
}