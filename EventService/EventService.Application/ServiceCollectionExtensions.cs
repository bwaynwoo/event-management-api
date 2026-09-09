using EventService.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EventCacheOptions>(options => 
        {
            configuration.GetSection("EventCache").Bind(options);
        });

        services.AddScoped<IEventService, Services.EventService>();

        return services;
    }
}