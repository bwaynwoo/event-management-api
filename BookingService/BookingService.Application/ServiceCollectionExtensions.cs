using BookingService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, Services.BookingService>();
        services.AddHostedService<BookingBackgroundService>();

        return services;
    }
}