using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Endpoints;

internal static class AuthEndpoints
{
    internal static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
                string login, string password,
                IUserService userService,
                CancellationToken cancellationToken) =>
            {
                var userInfo = await userService.RegisterAsync(
                    login,
                    password,
                    cancellationToken);

                return Results.Created($"/users/{userInfo.Id}", userInfo);
            })
            .WithName("Register")
            .Produces<UserInfo>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        app.MapPost("/auth/login", async (
                string login, string password,
                IUserService userService,
                CancellationToken cancellationToken) =>
            {
                var token = await userService.LoginAsync(
                    login,
                    password,
                    cancellationToken);

                return Results.Ok(new { token });
            })
            .WithName("Login")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        app.MapPost("/auth/register-admin", async (
                string login, string password,
                IUserService userService,
                CancellationToken cancellationToken) =>
            {
                var userInfo = await userService.RegisterAdminAsync(
                    login,
                    password,
                    cancellationToken);

                return Results.Created($"/users/{userInfo.Id}", userInfo);
            })
            .RequireAuthorization("Admin");

        return app;
    }
}