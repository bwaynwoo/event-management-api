using EventService.Application.Repositories;
using EventService.Infrastructure.DataAccess;
using EventService.Infrastructure.Kafka;
using EventService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace EventService.Infrastructure
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

            services.AddScoped<IEventRepository, EventRepository>();

            var redisConnectionString = configuration.GetSection("Redis")["ConnectionString"] ?? "localhost:6379";
            services.AddSingleton<IConnectionMultiplexer>(
                ConnectionMultiplexer.Connect(redisConnectionString));

            services.AddHostedService<KafkaTopicInitializer>();
            services.AddHostedService<BookingConfirmedConsumer>();

            return services;
        }
    }
}