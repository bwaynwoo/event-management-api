using BookingService.Application.Repositories;
using BookingService.Application.Services;
using BookingService.Infrastructure.DataAccess;
using BookingService.Infrastructure.Kafka;
using BookingService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<KafkaOptions>(configuration.GetSection("Kafka"));

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));
            services.AddScoped<IBookingRepository, BookingRepository>();

            services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

            return services;
        }
    }
}