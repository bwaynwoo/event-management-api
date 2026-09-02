using Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Services;

namespace UserService.Presentation.Endpoints;

internal static class AuthEndpoints
{
    internal static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/auth/register", async (
                LoginRequest request,
                IUserService userService,
                CancellationToken cancellationToken) =>
            {
                var userInfo = await userService.RegisterAsync(
                    request.Login,
                    request.Password,
                    cancellationToken);

                return Results.Created($"/users/{userInfo.Id}", userInfo);
            })
            .WithName("Register")
            .Produces<UserInfo>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        app.MapPost("/auth/login", async (
                LoginRequest request,
                IUserService userService,
                CancellationToken cancellationToken) =>
            {
                var token = await userService.LoginAsync(
                    request.Login,
                    request.Password,
                    cancellationToken);

                return Results.Ok(new { token });
            })
            .WithName("Login")
            .Produces(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        app.MapPost("/auth/register-admin", async (
                LoginRequest request,
                IUserService userService,
                CancellationToken cancellationToken) =>
            {
                var userInfo = await userService.RegisterAdminAsync(
                    request.Login,
                    request.Password,
                    cancellationToken);

                return Results.Created($"/users/{userInfo.Id}", userInfo);
            })
            .RequireAuthorization("Admin");

        return app;
    }
}

internal record LoginRequest(string Login, string Password);