using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SemaphoreSlim>(_ => new SemaphoreSlim(1, 1));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IBookingService, BookingService>();
            
        services.AddHostedService<BookingBackgroundService>();
            
        return services;
    }
}