using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using UserService.Application.Services;

namespace UserService.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<SemaphoreSlim>(_ => new SemaphoreSlim(1, 1));
        services.AddSingleton<PasswordHasher<object>>();

        services.AddScoped<IUserService, Services.UserService>();

        return services;
    }
}